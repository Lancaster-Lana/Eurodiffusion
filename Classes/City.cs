using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace EurodiffusionApp
{

    public delegate void CompleteCityHandler(City city);

    public class City
    {
        public event CompleteCityHandler CityCompleteEvent;

        #region Variables

        private string _name;
        private int _x = 0;
        private int _y = 0;
        private Country _country;
        private Hashtable cityCoins;
        private Hashtable exportCityCoins;
        private Hashtable importCityCoins;
        private City[] _neighbors = new City[4];
        private int _dayDiffComplete = 0;
        private int _daysCount = 0;

        #endregion

        #region Properties

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public int Xl
        {
            get { return _x; }
            set { _x = value; }
        }

        public int Yl
        {
            get { return _y; }
            set { _y = value; }
        }

        public Country Country
        {
            get { return _country; }
            set { _country = value; }
        }

        public City TopNeighbor
        {
            get { return _neighbors[0]; }
            set { _neighbors[0] = value; }
        }

        public City BottomNeighbor
        {
            get { return _neighbors[2]; }
            set { _neighbors[2] = value; }
        }

        public City LeftNeighbor
        {
            get { return _neighbors[3]; }
            set { _neighbors[3] = value; }
        }

        public City RightNeighbor
        {
            get { return _neighbors[1]; }
            set { _neighbors[1] = value; }
        }

        public int DayComplete
        {
            get { return _dayDiffComplete; }
            set { _dayDiffComplete = value; }
        }

        #endregion

        private bool ExportTo(City clientCity, int coinsCount, Country country)
        {
            if ((clientCity != null) && (clientCity.Country != null))
            {
                if (clientCity.importCityCoins.ContainsKey(country))
                    clientCity.importCityCoins[country] = coinsCount;
                else
                    clientCity.importCityCoins.Add(country, coinsCount);

                return true;
            }
            return false;
        }

        public City()
        {
            this.Name = "";
            this.Xl = -1;
            this.Yl = -1;
            this.Country = null;
        }

        public City(string name, Country country, int Xl, int Yl)
        {
            this.Name = name;
            this.Xl = Xl;
            this.Yl = Yl;
            this.Country = country;
            InitCityBudget(1000000);
        }

        private void InitCityBudget(int coinsCount)
        {
            DayComplete = -1;
            cityCoins = new Hashtable();
            importCityCoins = new Hashtable();
            cityCoins.Add(this.Country, coinsCount);
        }

        public void ExportCoins()
        {
            exportCityCoins = new Hashtable();

            IEnumerator ien = cityCoins.Keys.GetEnumerator();
            while (ien.MoveNext())
            {
                Country country = (Country)(ien.Current);
                int countCountryCoints = (int)cityCoins[country];
                ///
                int exportCount = countCountryCoints / 1000;
                int clients = 0;
                if (ExportTo(this.LeftNeighbor, exportCount, country)) clients++;
                if (ExportTo(this.RightNeighbor, exportCount, country)) clients++;
                if (ExportTo(this.TopNeighbor, exportCount, country)) clients++;
                if (ExportTo(this.BottomNeighbor, exportCount, country)) clients++;
                ///
                exportCityCoins.Add(country, clients * exportCount);
            }

            ien = exportCityCoins.Keys.GetEnumerator();
            while (ien.MoveNext())
            {
                Country country = (Country)(ien.Current);
                int countCountryCoints = (int)cityCoins[country];
                cityCoins[country] = (int)cityCoins[country] - (int)exportCityCoins[country];
            }
        }

        public void SummaryDayBudget()//For each city at start day
        {
            ///Summary import coints
            IEnumerator ien = importCityCoins.Keys.GetEnumerator();
            while (ien.MoveNext())
            {
                Country country = (Country)(ien.Current);
                int countCoints = (int)importCityCoins[country];

                if (cityCoins.Contains(country))
                    cityCoins[country] = (int)cityCoins[country] + countCoints;
                else
                    cityCoins.Add(country, countCoints);
            }

            //Increment days
            _daysCount++;

            //Check City Complete            
            if (DayComplete < 0)
            {
                if (IsComplete())
                {
                    CityCompleteEvent(this);
                }
            }
        }

        public bool IsComplete()
        {
            if (DayComplete >= 0) return true;

            IEnumerator ien = Country.ListAllCountries.GetEnumerator();
            while (ien.MoveNext())
            {
                Country country = (Country)(ien.Current);
                if (cityCoins[country] == null)
                    return false;
            }
            return true;
        }

    }
}
