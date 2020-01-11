<?php
	$server = "localhost";
	$dbName = "ecobuilder";
	$user = "root";
	$pass = "";
	
	$conn = new mysqli($server, $user, $pass, $dbName);
	if ($conn->connect_errno)
	{
        echo $conn->connect_error;
        exit();
    }

    $username = $_POST["username"];
    $password = $_POST['password'];

    $result = $conn->query("SELECT 1 FROM players WHERE username='".$username."'");
    if ($result->num_rows != 0)
    {
        echo "username taken";
        exit();
    }
    else
    {
        $hashed = password_hash($password, PASSWORD_DEFAULT);
        $result = $conn->query("INSERT INTO players (username, passwordHash)
                                VALUES ('".$username."','".$hashed."')");
        if ($result === TRUE)
        {
            echo "success!";
        }
        else
        {
            echo "error";
        }
    }
    $conn->close();
?>