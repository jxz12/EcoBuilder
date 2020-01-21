<?php
    include 'security.php';
    $rsa = InitRSA();
    $username = Decrypt($rsa, $_POST['username']);

    $sql = InitSQL();
    $stmt = $sql->prepare('SELECT * FROM players WHERE username=?');
    $stmt->bind_param('s', $username);
    $stmt->execute();
    if ($stmt->fetch() == TRUE) {
        http_response_code(409);
        die();
    }
    $stmt->close();

    // TODO: check for 'ddos' if someone is trying to just fill out the database?
    $password = Decrypt($rsa, $_POST['password']);
    $hashed = password_hash($password, PASSWORD_DEFAULT);
    $email = Decrypt($rsa, $_POST['email']);
    $stmt = $sql->prepare("INSERT INTO players (username, password_hash, email) VALUES (?,?,?);");
    $stmt->bind_param('sss', $username, $hashed, $email);
    $stmt->execute();
    if ($stmt->affected_rows > 0) {
        echo 'success!';
    } else {
        http_response_code(503); // should never happen if we've gotten this far
        die();
    }
    $stmt->close();

    $sql->close();
?>