<?php
    $creds = simplexml_load_file('./sql.xml');
	$sql = new mysqli($creds->server, $creds->user, $creds->pass, $creds->dbName);
	if ($sql->connect_errno)
	{
        echo $sql->connect_error;
        exit();
    }
    echo 'success';
?>