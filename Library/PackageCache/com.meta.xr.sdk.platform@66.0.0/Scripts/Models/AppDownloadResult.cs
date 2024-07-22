// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class AppDownloadResult
  {
    /// Result of the download or install operation returned by the installer
    public readonly AppInstallResult AppInstallResult;
    /// Timestamp in milliseconds when the operation finished.
    public readonly long Timestamp;


    public AppDownloadResult(IntPtr o)
    {
      AppInstallResult = CAPI.ovr_AppDownloadResult_GetAppInstallResult(o);
      Timestamp = CAPI.ovr_AppDownloadResult_GetTimestamp(o);
    }
  }

}
