using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Xml;

namespace EurodiffusionApp
{
    public delegate void OutputHander();
    public delegate void WriteCityHandler(City city);
    public delegate void InitHandler(string str);

    public partial class EuroDiffusionForm : Form
    {
        #region Variables

        protected Graphics _graph;
        private const int _cityWidth = 20;
        private const int _cityHeight = 20;
        private int _dayNumber = 0;
        private Thread _playThread;
        private bool _stopThread = false;
        private bool _areAllCountriesComplete = false;
        private int _countCities;
        private int _countCompleteCities;


        #endregion

        #region Form

        public EuroDiffusionForm()
        {
            InitializeComponent();
            System.Diagnostics.TextWriterTraceListener tl = new System.Diagnostics.TextWriterTraceListener(@"c:\tmp\trace.txt");
            System.Diagnostics.Trace.Listeners.Add(tl);
        }

        private void EuroDiffusionForm_Load(object sender, EventArgs e)
        {
            _graph = CitiesPB.CreateGraphics();
        }

        private void EurodiffusionForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _stopThread = true;
        }

        private void InputTC_SelectedIndexChanged(object sender, EventArgs e)
        {
            ResultsTC.SelectedIndex = InputTC.SelectedIndex;
        }

        private void CompleteTimer_Tick(object sender, EventArgs e)
        {
            ProgressAdd();
            CountDaysLabel.Text = _dayNumber.ToString() + " days";
        }

        private void countriesListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string c_name = countriesListBox.SelectedItem.ToString();
            //lock (Country.ListAllCountries)
            foreach (Country cnt in Country.ListAllCountries)
            {
                if (cnt.Name == c_name)
                {
                    XlUD.Value = cnt.Xl;
                    XHUD.Value = cnt.Xh;
                    YlUD.Value = cnt.Yl;
                    YHUD.Value = cnt.Yh;
                    ColorPanel.BackColor = cnt.Color;
                    break;
                }
            }

        }

        private void ResultsTC_SelectedIndexChanged(object sender, EventArgs e)
        {
            InputTC.SelectedIndex = ResultsTC.SelectedIndex;
        }

        #endregion

        #region Methods

        #region Draw

        private void DrawCountry(Country country)
        {
            Rectangle rect = new Rectangle(
                                    country.Xl * _cityWidth,
                                    country.Yl * _cityHeight,
                                    (country.Xh - country.Xl) * _cityWidth,
                                    (country.Yh - country.Yl) * _cityHeight
                                    );
            using (Pen pen = new Pen(new SolidBrush(country.Color)))
            {
                _graph.DrawRectangle(pen, rect);
                DrawCities(country);
            }
        }

        public void DrawCities(Country country)
        {
            using (Pen pen = new Pen(new SolidBrush(country.Color)))
            {
                for (int i = 0; i < (country.Xh - country.Xl); i++)
                    for (int j = 0; j < (country.Yh - country.Yl); j++)
                    {
                        Rectangle rectCity = new Rectangle(
                            _cityWidth * (country.Xl + i),
                            _cityHeight * (country.Yl + j),
                            _cityWidth, _cityHeight);
                        _graph.DrawEllipse(pen, rectCity);
                    }
            }
        }

        private void RefreshCountries(List<Country> list)
        {
            foreach (Country cnt in list)
            {
                DrawCountry(cnt);
                foreach (City city in cnt.CitiesList)
                {
                    if (city.IsComplete())
                    {
                        Rectangle rectCity = new Rectangle(
                                                _cityWidth * city.Xl,
                                                _cityHeight * city.Yl,
                                                _cityWidth, _cityHeight);
                        using (Brush brush = new SolidBrush(city.Country.Color))
                        {
                            _graph.FillEllipse(brush, rectCity);
                        }
                    }
                }
            }
        }

        #endregion

        #region Events

        public void OnCityComplete(City city)//Call At external module
        {
            //CitiesPB.Invoke(new MethodInvoker(delegate{}));
            try
            {
                city.DayComplete = _dayNumber;
                WriteCityHandler deleg = new WriteCityHandler(WriteCityResult);
                //1. deleg.Invoke(city);
                //2. object obj = null;
                //IAsyncResult res = deleg.BeginInvoke(city, null, obj);
                //bool returnValue = false;
                //deleg.EndInvoke(res);
                //3.                
                Invoke(deleg, city);
                _countCompleteCities++;
            }
            catch (Exception ex)
            {
                ErrorLabel.Text = ex.Message;
            }
        }

        public void OnCountryComplete(Country country)
        {
            try
            {
                country.DayComplete = _dayNumber;
                if (country.CitiesList.Count == 1)
                    country.DayComplete = 0;
            }
            catch (Exception ex)
            {
                ErrorLabel.Text = ex.Message;
            }
        }

        #endregion

        private void InitStates(string type)
        {
            switch (type)
            {
                case CommandType.Test:
                    //Init var 
                    _countCompleteCities = 0;
                    _dayNumber = 0;
                    _graph.Clear(CitiesPB.BackColor);
                    Country.ListAllCountries.Clear();
                    //Menu
                    menuItemRun.Enabled = true;
                    //Controls
                    InputTC.SelectedIndex = 0;
                    RunningPB.Value = 0;
                    StatusLabel.Text = "";
                    CountDaysLabel.Text = "";
                    RunBT.Enabled = true;
                    ResultsTB.Clear();
                    CitiesResultTB.Clear();
                    CCDataGridView.DataSource = null;
                    break;

                case CommandType.Run:
                    //Init var
                    _areAllCountriesComplete = false;
                    _countCompleteCities = 0;
                    _dayNumber = 0;
                    string inputStr = "\r\n______Case " + Country.ListAllCountries.Count + "_____\r\n";
                    foreach (Country cnt in Country.ListAllCountries)
                    {
                        inputStr += cnt.Name
                            + " :" + cnt.Xl.ToString()
                            + "  " + cnt.Yl.ToString()
                            + "  " + cnt.Xh.ToString()
                            + "  " + cnt.Yh.ToString()
                            + "\r\n";
                    }
                    WriteCaseInfo(inputStr);
                    //Menu                    
                    menuItemRun.Enabled = false;
                    menuItemStop.Enabled = true;
                    TestsToolStrip.Enabled = false;
                    //Controls
                    InputTC.SelectedIndex = 1;
                    RunningPB.Value = 0;
                    StatusLabel.Text = " RUNNING ";
                    RunningPB.Maximum = _countCities;
                    RunBT.Enabled = false;
                    break;

                case CommandType.FinishDiffusion:
                    //Menu                    
                    menuItemRun.Enabled = false;
                    menuItemStop.Enabled = false;
                    TestsToolStrip.Enabled = true;
                    //Controls                                                      
                    RunBT.Enabled = false;
                    StatusLabel.Text = "COMPLETE";
                    break;

                case CommandType.Stop:
                    //Menu                                        
                    menuItemStop.Enabled = false;
                    TestsToolStrip.Enabled = true;
                    //Controls                                                            
                    RunningPB.Value = 0;
                    StatusLabel.Text = "BREAK";
                    break;
            }
        }

        private void ExecuteCommand(string command, params string[] param)
        {
            switch (command)
            {
                case CommandType.Test:
                    InitStates(command);
                    LoadCountries(Convert.ToInt32(param[0]));
                    break;

                case CommandType.Run:
                    InitStates(CommandType.Run);
                    StartEuroDiffussion();
                    break;

                case CommandType.Stop:
                    _stopThread = true;
                    if (_playThread != null)
                    {
                        if (_playThread.ThreadState == ThreadState.Running)
                            _playThread.Abort();
                        _playThread.Join();
                    }
                    WriteCaseInfo(" !!! BREAK \r\n");
                    ExecuteCommand(CommandType.FinishDiffusion);
                    InitStates(CommandType.Stop);
                    break;

                case CommandType.FinishDiffusion:
                    //new InitHandler(InitStates).BeginInvoke(command, null, null);
                    //InitStates(command);
                    //GetCommonResults();
                    Invoke(new InitHandler(InitStates), command);
                    Invoke(new OutputHander(GetCommonResults));
                    break;

                case CommandType.Exit:
                    Application.Exit();
                    break;
            }
        }

        private void StartEuroDiffussion()
        {
            _playThread = new Thread(new ThreadStart(Play));
            _playThread.Start();
        }

        private void StopPrevPlay()
        {
            ExecuteCommand(CommandType.Stop);
        }

        private void Play()
        {
            _stopThread = false;
            while (true)
            {
                if (_stopThread) break;
                if (_areAllCountriesComplete)
                {
                    ExecuteCommand(CommandType.FinishDiffusion);
                    break;
                }
                _dayNumber++;

                PlayDay();
            }
        }

        private void PlayDay()
        {
            try
            {
                if (_stopThread) return;
                //Export
                lock (Country.ListAllCountries)
                    foreach (Country country in Country.ListAllCountries)
                    {
                        foreach (City city in country.CitiesList)
                        {
                            if (_stopThread) return;
                            city.ExportCoins();
                        }
                    }
                //Thread.Sleep(1);

                //Import
                int complCountrisCount = 0;

                foreach (Country country in Country.ListAllCountries)
                {
                    int complCitiesCount = 0;
                    foreach (City city in country.CitiesList)
                    {
                        if (_stopThread) return;
                        city.SummaryDayBudget();

                        if (city.IsComplete())
                        {
                            complCitiesCount++;
                        }
                        Thread.Sleep(100);
                    }

                    if ((country.DayComplete < 0) && (complCitiesCount == country.CitiesList.Count))
                    {
                        country.GenerateCompleteEvent();
                    }

                    if (country.IsComplete()) complCountrisCount++;
                }

                //Common servey of results
                if (complCountrisCount == Country.ListAllCountries.Count)
                {
                    _areAllCountriesComplete = true;
                }
            }
            catch (Exception ex)
            {
                ErrorLabel.Text = ex.Message;
            }
        }

        private void LoadCountries(int count)
        {
            //_1_Load countries info
            try
            {
                XmlDataDocument doc = new XmlDataDocument();
                string fileName = @"..\Input\Sample" + count.ToString() + ".xml";
                doc.Load(fileName);

                XmlNodeList list = doc.GetElementsByTagName("country");
                foreach (XmlNode node in list)
                {
                    string name = node.SelectSingleNode("Name").InnerText;
                    string Xl = node.SelectSingleNode("Xl").InnerText;
                    string Yl = node.SelectSingleNode("Yl").InnerText;
                    string Xh = node.SelectSingleNode("Xh").InnerText;
                    string Yh = node.SelectSingleNode("Yh").InnerText;
                    string color = node.SelectSingleNode("Color").InnerText;
                    Country cnt = new Country(name,
                           Convert.ToInt32(Xl),
                           Convert.ToInt32(Yl),
                           Convert.ToInt32(Xh),
                           Convert.ToInt32(Yh),
                           Color.FromName(color)
                        );
                    DrawCountry(cnt);
                    Country.ListAllCountries.Add(cnt);
                }

                //_2
                Country.FindCountriesLimits(Country.ListAllCountries);// FindCountriesLimits();                        

                //_3_Event complete subscribe
                _countCities = 0;

                foreach (Country country in Country.ListAllCountries)
                {
                    country.CountryCompleteEvent += new CompleteCountryHandler(OnCountryComplete);
                    foreach (City city in country.CitiesList)
                    {
                        city.CityCompleteEvent += new CompleteCityHandler(OnCityComplete);
                        _countCities++;
                    }
                }

                //_4_
                LoadCountriesInfo(Country.ListAllCountries);

            }
            catch (Exception ex)
            {
                ErrorLabel.Text = ex.Message;
            }
        }

        private void LoadCountriesInfo(List<Country> list)
        {
            countriesListBox.Items.Clear();

            foreach (Country cnt in list)
            {
                object obj = new object();
                countriesListBox.Items.Add(cnt.Name);
            }

            if (countriesListBox.Items.Count > 0)
                countriesListBox.SelectedIndex = 0;
        }

        private void GetCommonResults()
        {
            try
            {
                //_1_for current sample     
                List<Country> c_list = Country.ListAllCountries;

                DataSet ds = new DataSet();
                ds.Tables.Add("countries");
                DataColumn col1 = ds.Tables["countries"].Columns.Add("country", typeof(string));
                DataColumn col2 = ds.Tables["countries"].Columns.Add("day", typeof(int));

                foreach (Country country in c_list)
                {
                    DataRow row = ds.Tables["countries"].NewRow();
                    row["country"] = country.Name.Trim(); ;
                    row["day"] = country.DayComplete;
                    ds.Tables["countries"].Rows.Add(row);
                }


                DataRow[] rows = ds.Tables[0].Select("country like '%' ", "day ASC, country ASC");
                /*
                //_load GridView                
                DataSet newds = new DataSet();
                newds.Tables.Add("countries");
                newds.Tables["countries"].Columns.Add("country", typeof(string));
                newds.Tables["countries"].Columns.Add("day", typeof(int));
                newds.Merge(rows);

                CCDataGridView.DataSource = newds;
                 * CCDataGridView.DataMember = "countries";
                */

                ds.Tables[0].DefaultView.Sort = "day ASC, country ASC";
                CCDataGridView.DataSource = ds.Tables[0].DefaultView;


                //CCDataGridView.Sort(CCDataGridView.Columns["day"], ListSortDirection.Ascending);
                //CCDataGridView.Sort(CCDataGridView.Columns["country"], ListSortDirection.Ascending);                          

                //_2_for all samples                            

                StringBuilder sb = new StringBuilder();
                foreach (DataRow row in rows)//ds.Tables["countries"].
                {
                    sb.Append(row["country"] + " : " + row["day"] + "\r\n");
                }

                AllSamplesResultsTB.Text += "\r\n______Case "
                                            + Country.ListAllCountries.Count.ToString()
                                            + "______" + "\r\n";
                AllSamplesResultsTB.Text += sb.ToString();
            }
            catch (Exception ex)
            {
                ErrorLabel.Text = ex.Message;
            }
        }

        private void WriteCaseInfo(string str)
        {
            AllSamplesInputTB.Text += str;
        }

        private void WriteCityResult(City city)
        {
            //_Output
            string str = city.Country.Name + " : " + city.Name + " Day " + _dayNumber.ToString();
            ResultsTB.Text += str + "\r\n";

            //_2_Draw
            Rectangle rectCity = new Rectangle(
                        _cityWidth * city.Xl,
                        _cityHeight * city.Yl,
                        _cityWidth, _cityHeight);
            using (Brush brush = new SolidBrush(city.Country.Color))
            {
                _graph.FillEllipse(brush, rectCity);
            }
        }

        private void ProgressAdd()
        {
            if (RunningPB.Value == RunningPB.Maximum)
                RunningPB.Value = 0;

            RunningPB.Value = _countCompleteCities;
        }

        #endregion

        #region MenuStrip

        private void RunBT_Click(object sender, EventArgs e)
        {
            ExecuteCommand(CommandType.Run);
        }

        private void CCDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            List<Country> c_list = Country.ListAllCountries;
            string selCountryName = CCDataGridView.SelectedRows[0].Cells["country"].Value.ToString();
            Country currCountry = c_list[0];

            foreach (Country country in c_list)
            {
                if (country.Name == selCountryName)
                {
                    currCountry = country;
                    break;
                }
            }

            string outCitiesStr = currCountry.Name + "\r\n";
            outCitiesStr += "_______________\r\n";
            outCitiesStr += "CITY  :     DAY \r\n";
            outCitiesStr += "_______________\r\n";

            foreach (City city in currCountry.CitiesList)
            {
                outCitiesStr += city.Name + " : " + city.DayComplete.ToString() + " days \r\n";
            }

            CitiesResultTB.Text = outCitiesStr;
        }

        private void TestsToolStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            string name = e.ClickedItem.AccessibleName;
            string descr = e.ClickedItem.AccessibleDescription;
            ExecuteCommand(name, descr);
        }

        private void mainMenuStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            string name = e.ClickedItem.AccessibleName;
            string descr = e.ClickedItem.AccessibleDescription;
            ExecuteCommand(name, descr);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExecuteCommand(CommandType.Exit);
        }

        #endregion

        private void CitiesPB_Paint(object sender, PaintEventArgs e)
        {
            RefreshCountries(Country.ListAllCountries);
        }
    }
}
