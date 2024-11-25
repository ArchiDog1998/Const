// See https://aka.ms/new-console-template for more information

using ArchiToolkit.Test.PropDp;

var item = new PropTest();
item.Test = new()
{
    Test = default,
};

Console.WriteLine(item.Add);

item.Test = new();

item.Test.Test = new(12);

Console.WriteLine(item.Add);
