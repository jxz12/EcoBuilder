<?php
    include 'security.php';
    $rsa = InitRSA();
    $username = Decrypt($rsa, $_POST["username"]);
    $password = Decrypt($rsa, $_POST["password"]);

    $sql = InitSQL();
    VerifyLogin($sql, $username, $password);

    $index = Decrypt($rsa, $_POST["level_index"]);
    $ticks = Decrypt($rsa, $_POST["datetime_ticks"]);
    $score = Decrypt($rsa, $_POST["score"]);
    $matrix = $_POST["matrix"];
    $actions = $_POST["actions"];

    // insert into playthroughs
    $stmt = $sql->prepare("INSERT INTO playthroughs (username, level_index, datetime_ticks, matrix, actions, verified) VALUES (?,?,?,?,?,0);");
    $stmt->bind_param('siiss', $username, $index, $ticks, $matrix, $actions);
    $stmt->execute();
    if ($stmt->affected_rows > 0) {
        echo 'success!';
    } else {
        http_response_code(503);
        die();
    }
    $stmt->close();

    // insert into highscores
    $stmt = $sql->prepare('SELECT score_unverified FROM highscores WHERE username=? AND level_index=?');
    $stmt->bind_param('si', $username, $index);
    $stmt->bind_result($prev_score);
    $stmt->execute();
    $exists = $stmt->fetch();
    $stmt->close();
    if ($exists == TRUE)
    {
        // check and update if new high score
        if ($score > $prev_score)
        {
            $stmt = $sql->prepare('UPDATE highscores SET score_unverified=?, ticks_unverified=? WHERE username=? AND level_index=?;');
            $stmt->bind_param('iisi', $score, $ticks, $username, $password);
            $stmt->execute();
            if ($stmt->affected_rows > 0) {
                echo 'success!';
            } else {
                http_response_code(503); // should never happen if we've gotten this far
                die();
            }
            $stmt->close();
        }
    }
    else
    {
        // insert new row
        $stmt = $sql->prepare('INSERT INTO highscores (username, level_index, score_unverified, ticks_unverified) VALUES (?,?,?,?);');
        $stmt->bind_param('siii', $username, $index, $score, $ticks);
        $stmt->execute();
        if ($stmt->affected_rows > 0) {
            echo 'success!';
        } else {
            http_response_code(503); // should never happen if we've gotten this far
            die();
        }
        $stmt->close();
    }

    $sql->close();
?>