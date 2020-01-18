<?php
    include 'security.php';
    $rsa = InitRSA();
    $username = Decrypt($rsa, $_POST["username"]);
    $password = Decrypt($rsa, $_POST["password"]);
    $reversed = Decrypt($rsa, $_POST["reversed"]);

    $sql = InitSQL();
    VerifyLogin($sql, $username, $password);

    $stmt = $sql->prepare('UPDATE players SET reverseDrag=? WHERE username=?;');
    $stmt->bind_param('is', $reversed, $username);
    $stmt->execute();
    if ($stmt->affected_rows > 0) {
        echo 'success!';
    } else {
        http_response_code(503); // should never happen unless this is set twice
        die();
    }
    $sql->close();
?>