﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common
{
    public enum EnumMonth
    {
        January = 1,
        February = 2,
        March = 3,
        April = 4,
        May = 5,
        June = 6, 
        July = 7,
        August = 8,
        September = 9,
        October = 10,
        November = 11,
        December = 12
    }

    public enum EnumSeason
    {
        Spring,
        Summer,
        Fall,
        Winter
    }

    public enum EnumMoonPhase
    {
        Empty,
        Grow1,
        Grow2,
        Grow3,
        Full,
        Shrink1,
        Shrink2,
        Shrink3
    }

    public enum EnumHemisphere
    {
        North = 0,
        South = 2
    }

    public interface IClientGameCalendar : IGameCalendar
    {
        /// <summary>
        /// Returns a normalized vector of the sun position at the players current location
        /// </summary>
        Vec3f SunPositionNormalized { get; }

        /// <summary>
        /// Returns a vector of the sun position at the players current location
        /// </summary>
        Vec3f SunPosition { get; }

        /// <summary>
        /// Returns a vector of the moon position at the players current location
        /// </summary>
        Vec3f MoonPosition { get; }

        /// <summary>
        /// Returns a normalized color of the sun at the players current location
        /// </summary>
        Vec3f SunColor { get; }

        /// <summary>
        /// A horizontal offset that is applied when reading the sky glow color at the players current location. Creates a greater variety of sunsets. Changes to a different value once per day (during midnight)
        /// </summary>
        float SunsetMod { get; }

        /// <summary>
        /// Returns a value between 0 (no sunlight) and 1 (full sunlight) at the players current location
        /// </summary>
        /// <returns></returns>
        float DayLightStrength { get; }

        /// <summary>
        /// Returns a value between 0 (no sunlight) and 1 (full sunlight) at the players current location
        /// </summary>
        /// <returns></returns>
        float MoonLightStrength { get; }

        float SunLightStrength { get; }

        /// <summary>
        /// If true, its currently dusk at the players current location
        /// </summary>
        bool Dusk { get; }
    }


    /// <summary>
    /// Should return the suns vertical position in the sky from 0..1
    /// </summary>
    /// <param name="posX">World x coordinate</param>
    /// <param name="posZ">World z coordinate</param>
    /// <param name="yearRel">Current year progress, from 0..1</param>
    /// <param name="dayRel">Current day progress, from 0..1</param>
    /// <returns></returns>
    public delegate float SolarAltitudeDelegate(double posX, double posZ, float yearRel, float dayRel);

    public delegate EnumHemisphere HemisphereDelegate(double posX, double posZ);

    /// <summary>
    /// Main API for retrieving anything calender or astronomy related
    /// </summary>
    public interface IGameCalendar
    {
        /// <summary>
        /// Assigned by the survival mod. Must return the hemisphere at give location
        /// </summary>
        HemisphereDelegate OnGetHemisphere { get; set; }

        /// <summary>
        /// Assigned by the survival mod. The calendar uses this method to determine the solar altitude at given location and time. If set to null, the calendar uses a default value of about 0.9
        /// </summary>
        SolarAltitudeDelegate OnGetSolarAltitude { get; set; }

        /// <summary>
        /// Retrieve the current daylight strength at given coordinates
        /// </summary>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        float GetDayLightStrength(double x, double z);

        /// <summary>
        /// Retrieve the current daylight strength at given coordinates. The Y-Component is ignored
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        float GetDayLightStrength(BlockPos pos);


        /// <summary>
        /// The worlds current date, nicely formatted
        /// </summary>
        /// <returns></returns>
        string PrettyDate();

        /// <summary>
        /// This acts as a multiplier on how much faster an ingame second passes by compared to a real life second. Affects physics, like the motion speed of waving grass. The default is 60, hence per default a day lasts 24 minutes, but it's also multiplied by CalendarSpeedMul which is 0.5 by default so the end result is 48 minutes per day
        /// This is the sum of all modifiers
        /// </summary>
        float SpeedOfTime { get; }

        /// <summary>
        /// If you want to modify the time speed, set a value here
        /// </summary>
        void SetTimeSpeedModifier(string name, float speed);

        /// <summary>
        /// To remove a previously added time speed modifier
        /// </summary>
        /// <param name="name"></param>
        void RemoveTimeSpeedModifier(string name);

        /// <summary>
        /// A multiplier thats applied to the progression of the calendar. Set this to 0.1 and a day will last 10 times longer, does not affect physics.
        /// </summary>
        float CalendarSpeedMul { get; set; }

        /// <summary>
        /// Amount of hours per day
        /// </summary>
        float HoursPerDay { get; }

        /// <summary>
        /// Amount of days per year
        /// </summary>
        int DaysPerYear { get; }

        /// <summary>
        /// Amount of days per month
        /// </summary>
        int DaysPerMonth { get; }


        int Month { get; }
        EnumMonth MonthName { get; }

        /// <summary>
        /// The current hour of the day as integer
        /// </summary>
        int FullHourOfDay { get; }

        /// <summary>
        /// The current hour of the day as decimal 
        /// </summary>
        float HourOfDay { get; }

        /// <summary>
        /// Total passed hours since the game has started
        /// </summary>
        double TotalHours { get; }

        /// <summary>
        /// Total passed days since the game has started
        /// </summary>
        double TotalDays { get; }

        /// <summary>
        /// The current day of the year (goes from 0 to DaysPerYear)
        /// </summary>
        int DayOfYear { get; }

        
        /// <summary>
        /// Returns the year. Every game begins with 1386
        /// </summary>
        int Year { get; }

        /// <summary>
        /// Returns the season at given position
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        EnumSeason GetSeason(BlockPos pos);

        /// <summary>
        /// Returns the season at given position between 0..1
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        float GetSeasonRel(BlockPos pos);

        /// <summary>
        /// Returns the hemisphere at given position
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        EnumHemisphere GetHemisphere(BlockPos pos);

        /// <summary>
        /// Returns the current season in a value of 0 to 1
        /// </summary>
        float YearRel { get; }

        /// <summary>
        /// Adds given time to the calendar
        /// </summary>
        /// <param name="hours"></param>
        void Add(float hours);

        /// <summary>
        /// The current moonphase
        /// </summary>
        EnumMoonPhase MoonPhase { get; }
        /// <summary>
        /// The current moonphase represented by number from 0..8
        /// </summary>
        double MoonPhaseExact { get; }
        /// <summary>
        /// The moons current brightness (higher during full moon)
        /// </summary>
        float MoonPhaseBrightness { get; }
        /// <summary>
        /// The moons current size (larger during full moon)
        /// </summary>
        float MoonSize { get; }

        /// <summary>
        /// If non-null, will override the value retrieved by GetSeason(). Set to null to have seasons progress normally again.
        /// </summary>
        /// <param name="seasonRel"></param>
        void SetSeasonOverride(float? seasonRel);
    }
}
