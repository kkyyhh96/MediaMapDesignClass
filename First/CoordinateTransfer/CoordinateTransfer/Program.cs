﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoordinateTransfer
{
    class Program
    {
        static void Main(string[] args)
        {
            List<Gps> gpsList = new List<Gps>();
            //源数据和返回数据的路径
            string inputFilePath = @"D:\Users\KYH\Documents\Pycharm\MediaMapDesign\First\transfer_format.csv";
            string outputFilePath = @"D:\Users\KYH\Documents\Pycharm\MediaMapDesign\First\result.csv";

            PositionUtil positionUtil = new PositionUtil();
            //从源数据中将数据读取出来
            using (StreamReader sr = new StreamReader(inputFilePath))
            {
                Gps gps = new Gps(sr.ReadLine());
                while (gps != null)
                {
                    gps = PositionUtil.gcj_To_Gps84(gps.getWgLat(), gps.getWgLon());
                    gpsList.Add(gps);
                    string gpsLine = sr.ReadLine();
                    if (gpsLine != null)
                    {
                        gps = new Gps(gpsLine);
                    }
                    else
                    {
                        gps = null;
                    }
                }
            }

            //将转换后的数据写入文件中
            using (StreamWriter sw = new StreamWriter(outputFilePath))
            {
                for (int i = 0; i < gpsList.Count; i++)
                {
                    sw.WriteLine(gpsList[i].getWgLat().ToString() + "," + gpsList[i].getWgLon().ToString());
                }
            }

        }
        public class Gps
        {
            double latitude { set; get; }
            double longitude { set; get; }
            public Gps(double latitude, double lontitude)
            {
                this.latitude = latitude;
                this.longitude = lontitude;
            }
            public Gps(string gps)
            {
                this.latitude = Convert.ToDouble(gps.Split(',')[1]);
                this.longitude = Convert.ToDouble(gps.Split(',')[0]);
            }
            public double getWgLat()
            {
                return this.latitude;
            }
            public double getWgLon()
            {
                return this.longitude;
            }
        }

        /**
 * 各地图API坐标系统比较与转换;
 * WGS84坐标系：即地球坐标系，国际上通用的坐标系。设备一般包含GPS芯片或者北斗芯片获取的经纬度为WGS84地理坐标系,
 * 谷歌地图采用的是WGS84地理坐标系（中国范围除外）;
 * GCJ02坐标系：即火星坐标系，是由中国国家测绘局制订的地理信息系统的坐标系统。由WGS84坐标系经加密后的坐标系。
 * 谷歌中国地图和搜搜中国地图采用的是GCJ02地理坐标系; BD09坐标系：即百度坐标系，GCJ02坐标系经加密后的坐标系;
 * 搜狗坐标系、图吧坐标系等，估计也是在GCJ02基础上加密而成的。 chenhua
 */
        public class PositionUtil
        {

            public static String BAIDU_LBS_TYPE = "bd09ll";

            public static double pi = 3.1415926535897932384626;
            public static double a = 6378245.0;
            public static double ee = 0.00669342162296594323;

            /**
             * 84 to 火星坐标系 (GCJ-02) World Geodetic System ==> Mars Geodetic System
             * 
             * @param lat
             * @param lon
             * @return
             */
            public static Gps gps84_To_Gcj02(double lat, double lon)
            {
                if (outOfChina(lat, lon))
                {
                    return null;
                }
                double dLat = transformLat(lon - 105.0, lat - 35.0);
                double dLon = transformLon(lon - 105.0, lat - 35.0);
                double radLat = lat / 180.0 * pi;
                double magic = Math.Sin(radLat);
                magic = 1 - ee * magic * magic;
                double sqrtMagic = Math.Sqrt(magic);
                dLat = (dLat * 180.0) / ((a * (1 - ee)) / (magic * sqrtMagic) * pi);
                dLon = (dLon * 180.0) / (a / sqrtMagic * Math.Cos(radLat) * pi);
                double mgLat = lat + dLat;
                double mgLon = lon + dLon;
                return new Gps(mgLat, mgLon);
            }

            /**
             * * 火星坐标系 (GCJ-02) to 84 * * @param lon * @param lat * @return
             * */
            public static Gps gcj_To_Gps84(double lat, double lon)
            {
                Gps gps = transform(lat, lon);
                double lontitude = lon * 2 - gps.getWgLon();
                double latitude = lat * 2 - gps.getWgLat();
                return new Gps(latitude, lontitude);
            }

            /**
             * 火星坐标系 (GCJ-02) 与百度坐标系 (BD-09) 的转换算法 将 GCJ-02 坐标转换成 BD-09 坐标
             * 
             * @param gg_lat
             * @param gg_lon
             */
            public static Gps gcj02_To_Bd09(double gg_lat, double gg_lon)
            {
                double x = gg_lon, y = gg_lat;
                double z = Math.Sqrt(x * x + y * y) + 0.00002 * Math.Sin(y * pi);
                double theta = Math.Atan2(y, x) + 0.000003 * Math.Cos(x * pi);
                double bd_lon = z * Math.Cos(theta) + 0.0065;
                double bd_lat = z * Math.Sin(theta) + 0.006;
                return new Gps(bd_lat, bd_lon);
            }

            /**
             * * 火星坐标系 (GCJ-02) 与百度坐标系 (BD-09) 的转换算法 * * 将 BD-09 坐标转换成GCJ-02 坐标 * * @param
             * bd_lat * @param bd_lon * @return
             */
            public static Gps bd09_To_Gcj02(double bd_lat, double bd_lon)
            {
                double x = bd_lon - 0.0065, y = bd_lat - 0.006;
                double z = Math.Sqrt(x * x + y * y) - 0.00002 * Math.Sin(y * pi);
                double theta = Math.Atan2(y, x) - 0.000003 * Math.Cos(x * pi);
                double gg_lon = z * Math.Cos(theta);
                double gg_lat = z * Math.Sin(theta);
                return new Gps(gg_lat, gg_lon);
            }

            /**
             * (BD-09)-->84
             * @param bd_lat
             * @param bd_lon
             * @return
             */
            public static Gps bd09_To_Gps84(double bd_lat, double bd_lon)
            {

                Gps gcj02 = PositionUtil.bd09_To_Gcj02(bd_lat, bd_lon);
                Gps map84 = PositionUtil.gcj_To_Gps84(gcj02.getWgLat(),
                        gcj02.getWgLon());
                return map84;

            }

            public static Boolean outOfChina(double lat, double lon)
            {
                if (lon < 72.004 || lon > 137.8347)
                    return true;
                if (lat < 0.8293 || lat > 55.8271)
                    return true;
                return false;
            }

            public static Gps transform(double lat, double lon)
            {
                if (outOfChina(lat, lon))
                {
                    return new Gps(lat, lon);
                }
                double dLat = transformLat(lon - 105.0, lat - 35.0);
                double dLon = transformLon(lon - 105.0, lat - 35.0);
                double radLat = lat / 180.0 * pi;
                double magic = Math.Sin(radLat);
                magic = 1 - ee * magic * magic;
                double sqrtMagic = Math.Sqrt(magic);
                dLat = (dLat * 180.0) / ((a * (1 - ee)) / (magic * sqrtMagic) * pi);
                dLon = (dLon * 180.0) / (a / sqrtMagic * Math.Cos(radLat) * pi);
                double mgLat = lat + dLat;
                double mgLon = lon + dLon;
                return new Gps(mgLat, mgLon);
            }

            public static double transformLat(double x, double y)
            {
                double ret = -100.0 + 2.0 * x + 3.0 * y + 0.2 * y * y + 0.1 * x * y
                        + 0.2 * Math.Sqrt(Math.Abs(x));
                ret += (20.0 * Math.Sin(6.0 * x * pi) + 20.0 * Math.Sin(2.0 * x * pi)) * 2.0 / 3.0;
                ret += (20.0 * Math.Sin(y * pi) + 40.0 * Math.Sin(y / 3.0 * pi)) * 2.0 / 3.0;
                ret += (160.0 * Math.Sin(y / 12.0 * pi) + 320 * Math.Sin(y * pi / 30.0)) * 2.0 / 3.0;
                return ret;
            }

            public static double transformLon(double x, double y)
            {
                double ret = 300.0 + x + 2.0 * y + 0.1 * x * x + 0.1 * x * y + 0.1
                        * Math.Sqrt(Math.Abs(x));
                ret += (20.0 * Math.Sin(6.0 * x * pi) + 20.0 * Math.Sin(2.0 * x * pi)) * 2.0 / 3.0;
                ret += (20.0 * Math.Sin(x * pi) + 40.0 * Math.Sin(x / 3.0 * pi)) * 2.0 / 3.0;
                ret += (150.0 * Math.Sin(x / 12.0 * pi) + 300.0 * Math.Sin(x / 30.0
                        * pi)) * 2.0 / 3.0;
                return ret;
            }

        }
    }
}