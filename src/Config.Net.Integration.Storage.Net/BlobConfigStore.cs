﻿using Storage.Net.Blob;
using System;
using System.Collections.Generic;
using System.Text;

namespace Config.Net.Integration.Storage.Net
{
   class BlobConfigStore : IConfigStore
   {
      private readonly IBlobStorage _blobs;

      public BlobConfigStore(IBlobStorage blobs)
      {
         _blobs = blobs ?? throw new ArgumentNullException(nameof(blobs));
      }

      public string Name => "Storage.Net Blobs";

      public bool CanRead => true;

      public bool CanWrite => true;

      public void Dispose()
      {
      }

      public string Read(string key)
      {
         return _blobs.ReadText(key);
      }

      public void Write(string key, string value)
      {
         if (value == null)
         {
            _blobs.Delete(key);
         }
         else
         {
            _blobs.WriteText(key, value);
         }
      }
   }
}