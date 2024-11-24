// See https://aka.ms/new-console-template for more information

using ArchiToolkit.Test.PropDp;

var item = new PropTest();
item.Test = new()
{
    Test = 10,
};

Console.WriteLine(item.Add);

item.Test = new();

item.Test.Test = 20;

Console.WriteLine(item.Add);
