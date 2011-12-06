public static class StringCleanup
{
  public static string NormalizeName(this string str)
  {
   	return str.Trim(new char[]{'\u0019','\r','\n','\t',' '});
  }
}   
