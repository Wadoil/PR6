using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1.Pages
{
    internal class TimeHelper
    {
        public static string GetTimeOfDayGreeting()
        {
            var currentTime = DateTime.Now.TimeOfDay;

            if (currentTime >= TimeSpan.Parse("10:00") && currentTime <= TimeSpan.Parse("12:00"))
                return "Доброе утро";
            else if (currentTime >= TimeSpan.Parse("12:01") && currentTime <= TimeSpan.Parse("17:00"))
                return "Добрый день";
            else if (currentTime >= TimeSpan.Parse("17:01") && currentTime <= TimeSpan.Parse("19:00"))
                return "Добрый вечер";
            else
                return "Доброй ночи"; // для времени вне рабочих часов
        }   

        public static bool IsWithinWorkingHours()
        {
            var currentTime = DateTime.Now.TimeOfDay;
            var startTime = TimeSpan.Parse("10:00");
            var endTime = TimeSpan.Parse("19:00");

            return currentTime >= startTime && currentTime <= endTime;
        }
    }
}
