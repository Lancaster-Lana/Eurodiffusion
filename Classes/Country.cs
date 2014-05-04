using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text;
using System.Drawing;

namespace EurodiffusionApp
{
    public delegate void CompleteCountryHandler(Country country);   

    public class Country
    {
        public event CompleteCountryHandler CountryCompleteEvent;

        public static List<Country> ListAllCountries = new List<Country>();

        #region Variables

        private string _name;
        private int _x = 0;
        private int _y = 0;
        private int _xh = 0;
        private int _yh = 0;
        private Color _color;
        private int _dayComplete = -1;

        private List<City> _citiesList = new List<City>();

        #endregion 

        #region Properties

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

        public int Xh
        {
            get { return _xh; }
            set { _xh = value; }
        }

        public int Yh
        {
            get { return _yh; }
            set { _yh = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public List<City> CitiesList
        {
            get { return _citiesList; }
            set { _citiesList = value; }
        }

        public Color Color
        {
            get { return _color; }
            set { _color = value; }
        }

        public int DayComplete
        {
            get { return _dayComplete; }
            set { _dayComplete = value; }
        }

        #endregion

        public City this[int X, int Y]//Absolute coordinates
        {
            get 
            { 
                int k = (X - Xl) + (Y - Yl) * (Xh - Xl);            
                if((k >= 0)&&(k < _citiesList.Count))
                    return _citiesList[k];
                return new City();                
            }
            set
            {
                int k = (X - Xl) + (Y - Yl) * (Xh - Xl);
                _citiesList[k] = value;
            }
        }

        public Country(string name, int Xl, int Yl, int xh, int yh, Color color)
        {
            this.Name = name;
            this.Xl = Xl;
            this.Yl = Yl;
            this.Xh = xh;
            this.Yh = yh;
            this.Color = color;

            InitCities();
            SetCitiesNeighbors();
        }

        private void InitCities()
        {
            for (int j = Yl; j < Yh ; j++)
            {
                for (int i = Xl; i < Xh ; i++)
                {
                    int num = (i - Xl) + (j - Yl) * (Xh - Xl) + 1;

                    City city = new City("City_" + num.ToString(),
                                        this, i, j);
                    AddCity(city);                    
                }
            }
        }

        private void AddCity(City city)
        {
            CitiesList.Add(city);
        }

        private void SetCitiesNeighbors()
        {
            try
            {
                for (int x = Xl; x < Xh; x++)
                {
                    for (int y = Yl; y < Yh; y++)
                    {
                        if (y > Yl)
                            this[x, y].TopNeighbor = this[x, y - 1];
                        else
                            this[x, y].TopNeighbor = null;

                        if (x > Xl)
                            this[x, y].LeftNeighbor = this[x - 1, y];
                        else
                            this[x, y].LeftNeighbor = null;

                        if (y < Yh )
                            this[x, y].BottomNeighbor = this[x, y + 1];
                        else
                            this[x, y].BottomNeighbor = null;

                        if (x < Xh)
                            this[x, y].RightNeighbor = this[x + 1, y];
                        else
                            this[x, y].RightNeighbor = null;
                    }
                }
            }
            catch (Exception ex)
            {
             //   ErrorLabel.Text = ex.Message;
            }

        }

        private void SetBottomCountryNeighbor(Country country) 
        {
            int y = this.Yh;

            int X_From = country.Xl;
            int X_To = this.Xh;
            if (X_From > X_To)
            {
                X_From = X_To;
                X_To = country.Xl;
            }

            for (int x = X_From; x < X_To; x++)
            {               
                this[x, y - 1].BottomNeighbor = country[x, y];
                country[x, y].TopNeighbor = this[x, y - 1];
            }
        }

        private void SetRightCountryNeighbor(Country country)
        {
            int x = this.Xh;
            int Y_From = country.Yl;
            int Y_To = this.Yh;
            if (Y_From > Y_To)
            {
                Y_From = Y_To;
                Y_To = country.Yl;
            }

            for (int y = Y_From; y < Y_To; y++)
            {
                City cityRight = country[x, y];
                City cityLeft = this[x - 1, y];
                cityLeft.RightNeighbor = cityRight;
                cityRight.LeftNeighbor = cityLeft;
            }
        } 

        public static void FindCountriesLimits(List<Country> countriesList)//For define neighbors for each country
        {
            List<int> Vector_Xl = new List<int>();
            List<int> Vector_Xh = new List<int>();
            List<int> Vector_Yl = new List<int>();
            List<int> Vector_Yh = new List<int>();

            for (int i = 0; i < countriesList.Count; i++)
            {
                Vector_Xl.Add(countriesList[i].Xl);
                Vector_Xh.Add(countriesList[i].Xh);
                Vector_Yl.Add(countriesList[i].Yl);
                Vector_Yh.Add(countriesList[i].Yh);
            }

            for (int i = 0; i < countriesList.Count; i++)
            {
                Country currCountry = countriesList[i];

                int indexRightNeighbor = Vector_Xl.IndexOf(Vector_Xh[i]);
                if (indexRightNeighbor != -1)
                {
                    Country rightCountry = countriesList[indexRightNeighbor];
                    currCountry.SetRightCountryNeighbor(rightCountry);
                }

                int indexBottomNeighbor = Vector_Yl.IndexOf(Vector_Yh[i]);
                if (indexBottomNeighbor != -1)
                {
                    Country bottomCountry = countriesList[indexBottomNeighbor];
                    currCountry.SetBottomCountryNeighbor(bottomCountry);
                }

            }
        }

        public void GenerateCompleteEvent()
        {
            CountryCompleteEvent(this);
        }

        public bool IsComplete()
        {
            if (DayComplete >= 0) return true;
            return false;
        }

     }    
}
