<?php
    include 'security.php';
    $rsa = InitRSA();
    $username = Decrypt($rsa, $_POST["username"]);
    $password = Decrypt($rsa, $_POST["password"]);

    $sql = InitSQL();
    VerifyLogin($sql, $username, $password);

    $stmt = $sql->prepare('SELECT email, age, gender, education, team, reverse_drag FROM players WHERE username=?;');
    $stmt->bind_param('s', $username);
    $stmt->execute();
    $stmt->bind_result($email, $age, $gender, $education, $team, $reversed);
    if ($stmt->fetch() != TRUE)
    {
        http_response_code(503);
        exit();
    }
    echo $email.','.$age.','.$gender.','.$education.','.$team.','.$reversed;
    $sql->close();
?>