using BlazorDatasheet.Util;
using NUnit.Framework;

namespace BlazorDatasheet.Test;

public class UtilTests
{
    [Test]
    public void Max_Stack_Push_Peek_Tests()
    {
        var maxStack = new MaxStack<int>(2);
        maxStack.Push(1);
        Assert.AreEqual(1, maxStack.Peek());
        maxStack.Push(2);
        Assert.AreEqual(2, maxStack.Peek());
        maxStack.Push(3);
        Assert.AreEqual(3, maxStack.Peek());
        Assert.AreEqual(3, maxStack.Pop());
        Assert.AreEqual(2, maxStack.Pop());
        // Default integer because the first value was replaced
        Assert.AreEqual(0, maxStack.Pop());
    }

    [Test]
    public void Max_Stack_Push_Peek_Tests_string()
    {
        var maxStack = new MaxStack<string>(2);
        maxStack.Push("1");
        Assert.AreEqual("1", maxStack.Peek());
        maxStack.Push("2");
        Assert.AreEqual("2", maxStack.Peek());
        maxStack.Push("3");
        Assert.AreEqual("3", maxStack.Peek());
        Assert.AreEqual("3", maxStack.Pop());
        Assert.AreEqual("2", maxStack.Pop());
        // Default integer because the first value was replaced
        Assert.AreEqual(null, maxStack.Pop());
    }
}