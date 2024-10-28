﻿namespace weather_service.Models
{
    public class Weather
    {
        public Forecast forecast { get; set; }
    }

    public class Forecast
    {
        public List<ForecastDay> forecastday { get; set; }
    }

    public class ForecastDay
    {
        public Day day { get; set; }
    }

    public class Day
    {
        public float mintemp_c { get; set; }
        public float maxtemp_c { get; set; }
    }
}
