<?php
    include 'security.php';
    $rsa = InitRSA();
    $username = Decrypt($rsa, $_POST["username"]);
    $password = Decrypt($rsa, $_POST["password"]);

    $sql = InitSQL();
    VerifyLogin($sql, $username, $password);

    $age = Decrypt($rsa, $_POST["age"]);
    $gender = Decrypt($rsa, $_POST["gender"]);
    $education = Decrypt($rsa, $_POST["education"]);
    $stmt = $sql->prepare('UPDATE players SET age=?, gender=?, education=? WHERE username=?;');
    $stmt->bind_param('iiis', $age, $gender, $education, $username);
    $stmt->execute();
    if ($stmt->affected_rows > 0) {
        echo 'success!';
    } else {
        http_response_code(503); // should never happen unless this is set twice
        die();
    }
    $stmt->close();
    $sql->close();
?>