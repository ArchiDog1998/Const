﻿namespace Const.Test.Type;

public class TestClass : BaseConstClass
{
    public int Value { get; set; }

    public SubClass Sub { get; set; } = new();
}
