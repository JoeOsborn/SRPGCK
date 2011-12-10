using System.Text.RegularExpressions;

public static class StringCleanup
{
  public static string NormalizeName(this string str)
  {
		return RemoveControlCharacters(str).Trim();
  }
  public static string RemoveControlCharacters(this string str)
  {
		Regex reg = new Regex("[\x00-\x1f]");
   	return reg.Replace(str, "");
  }

}   
