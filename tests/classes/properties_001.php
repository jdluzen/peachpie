<?php

class Y {
	function foo() {
	}
}

class X {

	static $id = 0;

	function __construct () {
		@$this->id = 10;	// accessing static field with instance
		self::$id = 10;

		$a = new Y();
		$a->fld = $this;
		@$a->fld->id = 4;


		$v = "fld";
		$this->$v = 5;
	}
}

(new X);
