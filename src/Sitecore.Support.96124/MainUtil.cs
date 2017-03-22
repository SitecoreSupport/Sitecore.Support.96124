using Sitecore.Diagnostics.PerformanceCounters;
using System.Diagnostics;

namespace Sitecore.Support
{
  public class MainUtil
  {
    public static long GetPrivateBytesUsed()
    {
      try
      {
        Sitecore.Diagnostics.PerformanceCounters.PerformanceCounter counter = Sitecore.Diagnostics.PerformanceCounters.PerformanceCounter.CreateWindowsSystemCounter("Private Bytes", "Process");
        if (BaseCounter.Enabled)
        {
          while (counter.Value == 0L)
          {
          }
          return counter.Value;
        }
      }
      catch
      {
      }
      try
      {
        return Process.GetCurrentProcess().PagedMemorySize64;
      }
      catch
      {
      }
      return 0L;
    }
  }
}
