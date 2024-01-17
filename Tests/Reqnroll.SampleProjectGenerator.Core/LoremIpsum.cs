using System;
using System.Linq;

namespace Reqnroll.SampleProjectGenerator;

public class LoremIpsum
{
    private const string content =
        @"Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Suspendisse sit amet pulvinar nulla. Aliquam mi felis, elementum eu lacinia eu, aliquet in neque. Integer aliquet risus libero, in mollis augue scelerisque non. Curabitur ut sem porta, tempor justo id, tincidunt diam. Quisque euismod pharetra hendrerit. Nulla nec lectus at nulla fermentum pretium vitae blandit ipsum. Nam molestie ligula vitae volutpat eleifend. Duis sagittis risus a venenatis vulputate. Sed vestibulum cursus dictum. Mauris fermentum suscipit augue, nec ullamcorper eros suscipit nec. Aliquam commodo libero vitae sapien sodales, eget efficitur augue condimentum.

Quisque pellentesque a orci eu accumsan. Donec nec scelerisque tortor. Fusce sit amet risus et odio blandit laoreet. Aliquam nec risus et ante porttitor euismod. Donec dapibus eu metus nec commodo. Nunc eu tincidunt purus. Nulla a egestas augue. Curabitur molestie imperdiet ex sit amet aliquet. Phasellus ac dui vestibulum, blandit ligula id, pulvinar massa. Proin gravida tortor ipsum, in pretium eros consequat non. Morbi vel tristique neque. In in congue urna.

Maecenas enim ligula, tempus sit amet dolor in, molestie vulputate erat. Suspendisse luctus sodales augue, id iaculis massa molestie ut. Sed ex sem, elementum dignissim accumsan efficitur, fermentum id nulla. Vestibulum at mi in arcu egestas venenatis eget et leo. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Phasellus lectus nibh, mollis vitae molestie ut, molestie id nulla. In tempus justo elit, eu ultricies ipsum commodo at. Donec ac porta ante. Sed vel libero vitae sem consequat porta a sed massa. Vestibulum cursus felis tellus. Quisque lectus sapien, commodo eget elit a, laoreet placerat magna. Maecenas lacus ligula, varius non eros vel, lobortis venenatis diam. Sed sapien massa, dignissim eget massa et, tristique consequat sem. Suspendisse in malesuada enim. Maecenas diam risus, ultricies ac dictum sit amet, accumsan non orci. Suspendisse dui libero, viverra at orci vel, molestie bibendum ligula.

Curabitur egestas mi quis leo elementum malesuada. In sed lectus volutpat, sagittis nisi non, vehicula elit. Quisque eu pellentesque dui. Nunc ante lorem, ultricies vel vehicula eu, dignissim eget lectus. Suspendisse finibus, ex et scelerisque luctus, augue nisi finibus erat, sit amet efficitur ex nunc lobortis felis. Ut nec bibendum dolor. Phasellus elementum odio et tortor lobortis venenatis. Donec malesuada iaculis elit. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Suspendisse mattis risus a erat eleifend laoreet. Donec quam urna, vulputate ut lobortis id, auctor non lorem. Etiam imperdiet mauris in porta vulputate. Duis egestas felis a leo interdum congue.

Phasellus nec egestas nunc. Integer tempor tempor mi in sodales. Nulla eleifend enim eget dapibus tristique. Praesent blandit auctor justo, vitae condimentum lorem condimentum nec. Quisque auctor congue faucibus. Cras blandit purus vitae massa scelerisque, vitae commodo est egestas. Phasellus vitae magna in purus faucibus rhoncus sit amet quis nulla. In sollicitudin elementum scelerisque.

In eu feugiat lorem. Phasellus vitae eros pulvinar, dictum mi sed, dapibus tellus. Ut at diam viverra quam bibendum vulputate. Duis dapibus tortor ut lacus dapibus semper. Sed lacinia eleifend sapien, in cursus urna.";

    public static Random Rnd = new(2009);

    private static readonly string[] words;

    static LoremIpsum()
    {
        words = content.Split(new[] {' ', '\n', '\r', ',', '.'}, StringSplitOptions.RemoveEmptyEntries);
    }

    public static string GetWord() => words[Rnd.Next(words.Length)];

    public static string[] GetUniqueWords(int wordCount = 4, string wordPrefix = "")
    {
        var w = GetWords(wordCount, wordPrefix).Distinct(StringComparer.InvariantCultureIgnoreCase).ToArray();
        while (w.Length < wordCount)
            w = w.Concat(GetWords(wordCount - w.Length, wordPrefix)).Distinct(StringComparer.InvariantCultureIgnoreCase)
                .ToArray();
        return w;
    }

    public static string[] GetWords(int wordCount = 4, string wordPrefix = "")
    {
        return Enumerable.Range(0, wordCount).Select(i => wordPrefix + GetWord()).ToArray();
    }

    public static string GetShortText(int wordCount = 4, string wordPrefix = "") =>
        string.Join(" ", GetWords(wordCount, wordPrefix));

    public static T[] Randomize<T>(IEnumerable<T> input)
    {
        var result = new List<T>(input).ToArray();
        for (int i = 0; i < result.Length * 3; i++)
        {
            var i1 = Rnd.Next(result.Length);
            var i2 = Rnd.Next(result.Length);
            var v = result[i1];
            result[i1] = result[i2];
            result[i2] = v;
        }

        return result;
    }
}
