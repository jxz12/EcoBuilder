<?php
    use phpseclib\Crypt\RSA;
    function InitRSA()
    {
        set_include_path(get_include_path() . PATH_SEPARATOR . './phpseclib-2.0.23/phpseclib/');
        include 'Math/BigInteger.php';
        include 'Crypt/Hash.php';
        include 'Crypt/RSA.php';

        $rsa = new RSA();
        $rsa->loadKey(file_get_contents('./rsa.xml'), RSA::PRIVATE_FORMAT_XML);  
        //$rsa->setEncryptionMode(CRYPT_RSA_ENCRYPTION_PKCS1);
        return $rsa;
    }
    function Decrypt($rsa, $post)
    {
        $encrypted = strrev(base64_decode($post));
        return $rsa->decrypt($encrypted);
    }
    function InitSQL()
    {
        $creds = simplexml_load_file('./sql.xml');
        // mysqli_report(MYSQLI_REPORT_ERROR | MYSQLI_REPORT_STRICT);
        $sql = new mysqli($creds->server, $creds->user, $creds->pass, $creds->dbName);
        if ($sql->connect_errno)
        {
            http_response_code(503);
            die();
        }
        return $sql;
    }
    function VerifyLogin($sql, $username, $password)
    {
        $stmt = $sql->prepare('SELECT password_hash FROM players WHERE username=?;');
        $stmt->bind_param('s', $username);
        $stmt->execute();
        $hashed = "";
        $stmt->bind_result($hashed);
        if ($stmt->fetch() != TRUE)
        {
            http_response_code(401);
            exit();
        }
        if (!password_verify($password, $hashed))
        {
            http_response_code(401);
            exit();
        }
        $stmt->close();
    }
?>