<?php
    include 'security.php';
    $rsa = InitRSA();
    $username = Decrypt($rsa, $_POST["username"]);
    $password = Decrypt($rsa, $_POST["password"]);

    $sql = InitSQL();
    VerifyLogin($sql, $username, $password);

    $reversed = Decrypt($rsa, $_POST["reversed"]);
    $stmt = $sql->prepare('UPDATE players SET reverse_drag=? WHERE username=?;');
    $stmt->bind_param('is', $reversed, $username);
    $stmt->execute();
    if ($stmt->affected_rows > 0) {
        echo 'success!';
    } else {
        http_response_code(503); // should never happen unless this is set twice
        die();
    }
    $stml->close();
    $sql->close();
?>