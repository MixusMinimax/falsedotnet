namespace FalseDotNet.Utility;

public static class LinkedListExtensions
{
    public static T PopFront<T>(this LinkedList<T> list)
    {
        var ret = list.First();
        list.RemoveFirst();
        return ret;
    }
}