// Minimal stand-in for the NUnit API used by the HarborWatch EditMode tests,
// plus a reflection runner. Only used by tools/core-tests (outside Unity).
using System;
using System.Linq;
using System.Reflection;

namespace NUnit.Framework
{
    [AttributeUsage(AttributeTargets.Method)] public sealed class TestAttribute : Attribute { }
    public sealed class AssertionException : Exception { public AssertionException(string m) : base(m) { } }
    public sealed class IgnoreException : Exception { public IgnoreException(string m) : base(m) { } }

    public static class Assert
    {
        static void Fail(string what, string msg) { throw new AssertionException(what + (string.IsNullOrEmpty(msg) ? "" : " · " + msg)); }
        public static void AreEqual(object expected, object actual, string message = null)
        {
            if (!Equals(expected, actual) && !(IsNum(expected) && IsNum(actual) && Convert.ToDouble(expected) == Convert.ToDouble(actual)))
                Fail($"Expected {expected} but was {actual}", message);
        }
        public static void AreEqual(double expected, double actual, double delta, string message = null)
        {
            if (Math.Abs(expected - actual) > delta) Fail($"Expected {expected} ± {delta} but was {actual}", message);
        }
        public static void GreaterOrEqual(int a, int b, string message = null) { if (a < b) Fail($"Expected {a} >= {b}", message); }
        public static void IsTrue(bool c, string message = null) { if (!c) Fail("Expected true", message); }
        public static void Ignore(string message) { throw new IgnoreException(message); }
        static bool IsNum(object o) { return o is int || o is uint || o is long || o is double || o is float; }
    }

    public static class StringAssert
    {
        public static void Contains(string expected, string actual) { if (actual == null || !actual.Contains(expected)) throw new AssertionException($"'{actual}' does not contain '{expected}'"); }
        public static void EndsWith(string expected, string actual) { if (actual == null || !actual.EndsWith(expected)) throw new AssertionException($"'{actual}' does not end with '{expected}'"); }
    }

    public sealed class TestContext
    {
        public static TestContext CurrentContext { get; } = new TestContext();
        public string TestDirectory => AppContext.BaseDirectory;
    }
}

public static class ShimRunner
{
    public static int Main()
    {
        int pass = 0, fail = 0;
        var tests = typeof(ShimRunner).Assembly.GetTypes()
            .SelectMany(t => t.GetMethods().Where(m => m.GetCustomAttribute<NUnit.Framework.TestAttribute>() != null).Select(m => (t, m)));
        foreach (var (type, method) in tests)
        {
            try { method.Invoke(Activator.CreateInstance(type), null); pass++; Console.WriteLine("ok   " + type.Name + "." + method.Name); }
            catch (TargetInvocationException e) when (e.InnerException is NUnit.Framework.IgnoreException) { Console.WriteLine("skip " + method.Name + ": " + e.InnerException.Message); }
            catch (TargetInvocationException e) { fail++; Console.WriteLine("FAIL " + type.Name + "." + method.Name + ": " + e.InnerException?.Message); }
        }
        Console.WriteLine($"# pass {pass}  fail {fail}");
        return fail == 0 ? 0 : 1;
    }
}
