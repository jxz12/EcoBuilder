<?php
    set_include_path(get_include_path() . PATH_SEPARATOR . './phpseclib-2.0.23/phpseclib/');
    include 'Math/BigInteger.php';
    include 'Crypt/Hash.php';
    include 'Crypt/RSA.php';
    use phpseclib\Crypt\RSA;

    $usernameEncrypted = strrev(base64_decode($_POST["username"]));
    $passwordEncrypted = strrev(base64_decode($_POST["password"]));
    $ageEncrypted = strrev(base64_decode($_POST["age"]));
    $genderEncrypted = strrev(base64_decode($_POST["gender"]));
    $educationEncrypted = strrev(base64_decode($_POST["education"]));

    $rsa = new RSA();
    $rsa->loadKey(file_get_contents("./key.xml"), RSA::PRIVATE_FORMAT_XML);  
    //$rsa->setEncryptionMode(CRYPT_RSA_ENCRYPTION_PKCS1);
    $username = $rsa->decrypt($usernameEncrypted);
    $password = $rsa->decrypt($passwordEncrypted);
    $age = $rsa->decrypt($ageEncrypted);
    $gender = $rsa->decrypt($genderEncrypted);
    $education = $rsa->decrypt($educationEncrypted);

	$server = "localhost";
	$dbName = "ecobuilder";
	$user = "root";
	$pass = "";

	$sql = new mysqli($server, $user, $pass, $dbName);
	if ($sql->connect_errno)
	{
        echo $sql->connect_error;
        exit();
    }
    $result = $sql->query("SELECT passwordHash FROM players WHERE username='" . $username . "';");
    if ($result->num_rows == 0)
    {
        echo "username does not exist";
        exit();
    }

    $storedHash = $result->fetch_row()[0];
    $hashed = password_hash($password, PASSWORD_DEFAULT);
    if (!strcmp($hashed, $storedHash))
    {
        echo "password incorrect";
        exit();
    }
    $result = $sql->query("UPDATE players SET age=" . $age . ", gender=" . $gender . ", education=" . $education . " WHERE username = '" . $username . "';");
    if ($result === TRUE)
    {
        echo "success!";
    }
    else
    {
        echo "error";
    }
    $sql->close();
?>