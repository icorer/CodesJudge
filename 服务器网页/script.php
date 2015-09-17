<?php
	if($_GET['problemid']!="")
	{
		$problemid=$_GET['problemid'];
		$root="./";
		$dat=$root."data/".$problemid.".dat";
		$data=$root."data/".$problemid.".data";
		$info=$root."data/".$problemid.".info";
		if(file_exists($dat)&&file_exists($data)&&file_exists($info))
			print "yes";
		else
			print "no";
	}
	else
		print "LinkOk";
?>