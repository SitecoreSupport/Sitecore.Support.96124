using Sitecore.Caching;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Events.Hooks;
using Sitecore.Services;
using System;
using System.Globalization;

namespace Sitecore.Support.Diagnostics
{
  public class MemoryMonitorHook : IHook
  {
    private bool _adjustLoadFactor = true;
    private static AlarmClock _alarmClock;
    private bool _clearCaches = true;
    private bool _garbageCollect = true;
    private readonly TimeSpan _interval;
    private readonly TimeSpan _minimumLogInterval;
    private readonly long _threshold;
    private DateTime m_lastLogTime = DateTime.UtcNow;
    private int m_suppressedLogs;

    public MemoryMonitorHook(string threshold, string checkInterval, string minimumLogInterval)
    {
      Assert.ArgumentNotNullOrEmpty(threshold, "threshold");
      Assert.ArgumentNotNullOrEmpty(checkInterval, "checkInterval");
      Assert.ArgumentNotNullOrEmpty(minimumLogInterval, "minimumLogInterval");
      this._threshold = StringUtil.ParseSizeString(threshold);
      this._interval = TimeSpan.Parse(checkInterval);
      this._minimumLogInterval = TimeSpan.Parse(minimumLogInterval);
    }

    private void AlarmClock_Ring(object sender, EventArgs args)
    {
      long privateBytesUsed = Sitecore.Support.MainUtil.GetPrivateBytesUsed();
      if (privateBytesUsed > this._threshold)
      {
        if (this.ClearCaches)
        {
          CacheManager.ClearAllCaches();
        }
        if (this.GarbageCollect)
        {
          GC.Collect(GC.MaxGeneration);
        }
        if (this.AdjustLoadFactor)
        {
          double loadFactor = CacheManager.LoadFactor;
          if (loadFactor < Settings.Caching.MaxLoadFactor)
          {
            CacheManager.LoadFactor = loadFactor + 0.2;
          }
        }
        this.LogStatus(privateBytesUsed);
      }
    }

    public void Initialize()
    {
      if (_alarmClock == null)
      {
        _alarmClock = new AlarmClock(this._interval);
        _alarmClock.Ring += new EventHandler<EventArgs>(this.AlarmClock_Ring);
        Log.Info(string.Concat(new object[] { "MemoryMonitor initialized. Threshold: ", Sitecore.MainUtil.FormatSize(this._threshold), ". Interval: ", this._interval }), this);
      }
    }

    private void LogStatus(long usedBefore)
    {
      if ((DateTime.UtcNow - this.m_lastLogTime) < this._minimumLogInterval)
      {
        this.m_suppressedLogs++;
      }
      else
      {
        long privateBytesUsed = Sitecore.Support.MainUtil.GetPrivateBytesUsed();
        double loadFactor = CacheManager.LoadFactor;
        string message = "Memory usage exceeded the MemoryMonitor threshold.";
        if (this.ClearCaches)
        {
          message = message + " All caches have been cleared.";
        }
        if (this.GarbageCollect)
        {
          message = message + " Forced GC has been induced.";
        }
        Log.Warn(message, this);
        if (this.GarbageCollect)
        {
          Log.Warn("Memory used before/after GC: " + Sitecore.MainUtil.FormatLong(usedBefore) + " / " + Sitecore.MainUtil.FormatLong(privateBytesUsed), this);
        }
        else
        {
          Log.Warn("Memory usage: " + Sitecore.MainUtil.FormatLong(privateBytesUsed), this);
        }
        if (this.AdjustLoadFactor)
        {
          Log.Warn("The cache load factor have been increased to " + loadFactor.ToString(CultureInfo.InvariantCulture) + ".", this);
        }
        Log.Warn("Number of suppressed logs due to the minimum time between log entries: " + this.m_suppressedLogs, this);
        this.m_lastLogTime = DateTime.UtcNow;
        this.m_suppressedLogs = 0;
      }
    }

    public bool AdjustLoadFactor
    {
      get
      {
        return this._adjustLoadFactor;
      }
      set
      {
        this._adjustLoadFactor = value;
      }
    }


    public bool ClearCaches
    {
      get
      {
        return this._clearCaches;
      }
      set
      {
        this._clearCaches = value;
      }
    }


    public bool GarbageCollect
    {
      get
      {
        return this._garbageCollect;
      }
      set
      {
        this._garbageCollect = value;
      }
    }
  }

}
