using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace FoodShoppingApp.Tests;

public class Program
{
    public static int Main(string[] args)
    {
        Console.WriteLine("=================================================");
        Console.WriteLine("  URBAN FRESH - IN-PROCESS TEST SUITE RUNNER     ");
        Console.WriteLine("=================================================");

        var assembly = typeof(Program).Assembly;
        var testTypes = assembly.GetTypes()
            .Where(t => t.Namespace == "FoodShoppingApp.Tests"
                     && !t.IsAbstract
                     && t.IsClass
                     && !t.Name.StartsWith("<")
                     && t != typeof(Program))
            .OrderBy(t => t.Name);

        int total = 0;
        int passed = 0;
        int failed = 0;

        foreach (var type in testTypes)
        {
            Console.WriteLine($"\n[Test Suite] {type.Name}");
            object? instance = null;
            try
            {
                instance = Activator.CreateInstance(type);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [ERROR] Cannot instantiate fixture {type.Name}: {ex.Message}");
                continue;
            }

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var facts = method.GetCustomAttributes(typeof(FactAttribute), false);
                var theories = method.GetCustomAttributes(typeof(TheoryAttribute), false);

                if (facts.Length > 0 && theories.Length == 0)
                {
                    total++;
                    try
                    {
                        var result = method.Invoke(instance, null);
                        if (result is Task task)
                        {
                            task.GetAwaiter().GetResult();
                        }
                        Console.WriteLine($"  + PASS: {method.Name}");
                        passed++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  X FAIL: {method.Name} -> {ex.InnerException?.Message ?? ex.Message}");
                        failed++;
                    }
                }
                else if (theories.Length > 0)
                {
                    var parameters = method.GetParameters();
                    var inlineDataAttrs = method.GetCustomAttributes(typeof(InlineDataAttribute), false).Cast<InlineDataAttribute>().ToList();

                    foreach (var inlineData in inlineDataAttrs)
                    {
                        total++;
                        var rawParams = inlineData.GetData(method).FirstOrDefault() ?? Array.Empty<object>();
                        var convertedParams = new object?[parameters.Length];
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            if (i >= rawParams.Length || rawParams[i] == null)
                            {
                                convertedParams[i] = null;
                            }
                            else if (parameters[i].ParameterType == typeof(decimal))
                            {
                                convertedParams[i] = Convert.ToDecimal(rawParams[i]);
                            }
                            else
                            {
                                convertedParams[i] = Convert.ChangeType(rawParams[i], parameters[i].ParameterType);
                            }
                        }

                        var paramStr = string.Join(", ", convertedParams.Select(p => p?.ToString() ?? "null"));
                        try
                        {
                            var result = method.Invoke(instance, convertedParams);
                            if (result is Task task)
                            {
                                task.GetAwaiter().GetResult();
                            }
                            Console.WriteLine($"  + PASS: {method.Name}({paramStr})");
                            passed++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"  X FAIL: {method.Name}({paramStr}) -> {ex.InnerException?.Message ?? ex.Message}");
                            failed++;
                        }
                    }
                }
            }
        }

        Console.WriteLine("\n=================================================");
        Console.WriteLine($"TEST SUMMARY: {total} Executed | {passed} Passed | {failed} Failed");
        Console.WriteLine("=================================================");

        return failed > 0 ? 1 : 0;
    }
}
