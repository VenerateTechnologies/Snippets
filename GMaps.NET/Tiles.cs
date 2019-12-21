using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GMap.NET;
using GMap.NET.MapProviders;

namespace GreatMapsDemo
{
   class Program
   {
      static void Main(string[] args)
      {
         Console.WriteLine("Hello World!");

         var center_lat = 28.4852;
         var center_lon = -81.5521;

         CreateTileMappings(center_lat - 1, center_lon - 1, center_lat + 1, center_lon + 1);

         Console.ReadLine();
      }

      static void CreateTileMappings(double min_lat, double min_lon, double max_lat, double max_lon)
      {
         try
         {
            var objClient = new System.Net.WebClient();

            GMaps.Instance.Mode = AccessMode.ServerOnly;

            GMapProvider provider = GMapProviders.BingHybridMap;

            var objMapUrl = (MethodInfo)typeof(BingHybridMapProvider).GetMethod(@"MakeTileImageUrl", BindingFlags.NonPublic | BindingFlags.Instance);

            int zoom = 8;
            RectLatLng area = RectLatLng.FromLTRB(min_lon, max_lat, max_lon, min_lat);

            if (!area.IsEmpty)
            {
               try
               {
                  List<GPoint> tileArea = provider.Projection.GetAreaTileList(area, zoom, 0);

                  GPoint topLeftPx = provider.Projection.FromLatLngToPixel(area.LocationTopLeft, zoom);
                  GPoint rightBottomPx = provider.Projection.FromLatLngToPixel(area.Bottom, area.Right, zoom);
                  GPoint pxDelta = new GPoint(rightBottomPx.X - topLeftPx.X, rightBottomPx.Y - topLeftPx.Y);

                  var iTileWidth = (tileArea.Count / 2) * 256;

                  var objBitmap = new SkiaSharp.SKBitmap(iTileWidth, iTileWidth);
                  var objCanvas = new SkiaSharp.SKCanvas(objBitmap);

                  foreach (var p in tileArea)
                  {
                     var objResult = objMapUrl.Invoke(provider, new object[] { p, zoom, @"en" });
                     var strResult = objResult as string;

                     var objData = objClient.DownloadData(strResult);
                     var objTile = SkiaSharp.SKImage.FromEncodedData(objData);

                     int x = (int)(p.X * provider.Projection.TileSize.Width - topLeftPx.X);
                     int y = (int)(p.Y * provider.Projection.TileSize.Width - topLeftPx.Y);

                     objCanvas.DrawImage(objTile, new SkiaSharp.SKPoint(x, y));
                  }

                  var mapPath = Path.GetTempFileName();

                  objCanvas.Flush();
                  objCanvas.Save();

                  using (var objImage = SkiaSharp.SKImage.FromBitmap(objBitmap))
                  {
                     using (var data = objImage.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100))
                     {
                        using (MemoryStream ms = new MemoryStream())
                        {
                           data.SaveTo(ms);
                           File.WriteAllBytes(mapPath, ms.ToArray());
                        }
                     }
                  }

                  Console.WriteLine(mapPath);
               }
               catch
               {
               }
            }
         }
         catch
         {
         }
      }
   }
}
