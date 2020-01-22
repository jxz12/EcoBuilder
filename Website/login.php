<?php
    include 'security.php';
    $rsa = InitRSA();
    $username = Decrypt($rsa, $_POST["username"]);
    $password = Decrypt($rsa, $_POST["password"]);

    $sql = InitSQL();
    VerifyLogin($sql, $username, $password);

    // get team and reverse_drag
    $stmt = $sql->prepare('SELECT team, reverse_drag FROM players WHERE username=?;');
    $stmt->bind_param('s', $username);
    $stmt->execute();
    $stmt->bind_result($team, $reversed);
    if ($stmt->fetch() != TRUE)
    {
        http_response_code(503);
        exit();
    }
    $stmt->close();

    $return = $team.';'.$reversed;

    $stmt = $sql->prepare('SELECT level_index, score_unverified FROM highscores WHERE username=?;');
    $stmt->bind_param('s', $username);
    $stmt->execute();
    $stmt->bind_result($index, $score);
    while ($stmt->fetch() == TRUE)
    {
        $return .= ';'.$index.':'.$score;
    }
    echo $return;

    $sql->close();
?>