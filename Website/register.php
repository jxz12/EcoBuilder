<?php
    include 'security.php';
    $rsa = InitRSA();
    $username = Decrypt($rsa, $_POST['username']);
    $password = Decrypt($rsa, $_POST['password']);
    $email = Decrypt($rsa, $_POST['email']);

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
    $hashed = password_hash($password, PASSWORD_DEFAULT);
    $stmt = $sql->prepare("INSERT INTO players (username, passwordHash, email) VALUES (?,?,?);");
    $stmt->bind_param('sss', $username, $hashed, $email);
    $stmt->execute();
    if ($stmt->affected_rows > 0) {
        echo 'success!';
    } else {
        http_response_code(503); // should never happen if we've gotten this far
        die();
    }
    $sql->close();
?>