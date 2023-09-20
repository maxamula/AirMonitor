using MySql.Data.MySqlClient;
using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;
using System.Windows;
using System.Diagnostics;
using System.Collections.ObjectModel;
using HandyControl.Controls;

namespace AirMonitor
{
    public class Factory : VMBase
    {
        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        private string _desc;
        public string Desc
        {
            get => _desc;
            set
            {
                if (_desc != value)
                {
                    _desc = value;
                    OnPropertyChanged(nameof(Desc));
                }
            }
        }

        private float _lat;
        public float Lat
        {
            get => _lat;
            set
            {
                if (_lat != value)
                {
                    _lat = value;
                    OnPropertyChanged(nameof(Lat));
                }
            }
        }

        private float _lng;
        public float Lng
        {
            get => _lng;
            set
            {
                if (_lng != value)
                {
                    _lng = value;
                    OnPropertyChanged(nameof(Lng));
                }
            }
        }
    }

    public class Pollutant : VMBase
    {
        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        private int _dangerClass;
        public int DangerClass
        {
            get => _dangerClass;
            set
            {
                if (_dangerClass != value)
                {
                    _dangerClass = value;
                    OnPropertyChanged(nameof(DangerClass));
                }
            }
        }

        private int _maxAmount;
        public int MaxAmount
        {
            get => _maxAmount;
            set
            {
                if (_maxAmount != value)
                {
                    _maxAmount = value;
                    OnPropertyChanged(nameof(MaxAmount));
                }
            }
        }
    }

    public class Record : VMBase
    {
        private Factory _factory;
        public Factory Factory
        {
            get => _factory;
            set
            {
                if (_factory != value)
                {
                    _factory = value;
                    OnPropertyChanged(nameof(Factory));
                }
            }
        }

        private Pollutant _pollutant;
        public Pollutant Pollutant
        {
            get => _pollutant;
            set
            {
                if (_pollutant != value)
                {
                    _pollutant = value;
                    OnPropertyChanged(nameof(Pollutant));
                }
            }
        }

        private float _pollution;
        public float Pollution
        {
            get => _pollution;
            set
            {
                if (_pollution != value)
                {
                    _pollution = value;
                    OnPropertyChanged(nameof(Pollution));
                    OnPropertyChanged(nameof(IsExceed));
                }
            }
        }

        private int _year;
        public int Year
        {
            get => _year;
            set
            {
                if (_year != value)
                {
                    _year = value;
                    OnPropertyChanged(nameof(Year));
                }
            }
        }

        public bool IsExceed { get => Pollution > Pollutant.MaxAmount; }
    }

    public class Database : VMBase
    {
        public Database()
        {
            _connection = new MySqlConnection($"Server=localhost;Database=ecomon;User=root;Password=null;");
            _connection.Open();
        }

        public void Import(string file, int year)
        {
            try
            {
                using (var stream = File.Open(file, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        string factory = null;
                        int factoryId = -1;
                        while (reader.Read())
                        {
                            string temp = reader.GetString(0);
                            if (temp != null)
                            {
                                factory = temp;
                                using (MySqlCommand command = new MySqlCommand($"SELECT (ID) FROM objects WHERE object_name = @factory_name", _connection))
                                {
                                    command.Parameters.AddWithValue("@factory_name", factory);
                                    MySqlDataReader sqlreader = command.ExecuteReader();
                                    if (sqlreader.Read())
                                    {
                                        factoryId = sqlreader.GetInt32(0);
                                        sqlreader.Close();
                                    }
                                    else
                                    {
                                        sqlreader.Close();
                                        using (MySqlCommand addFactoryCommand = new MySqlCommand($"INSERT INTO objects (object_name) VALUES (@factory_name); SELECT LAST_INSERT_ID();", _connection))
                                        {
                                            addFactoryCommand.Parameters.AddWithValue("@factory_name", factory);
                                            factoryId = Convert.ToInt32(addFactoryCommand.ExecuteScalar());
                                        }
                                    }
                                }
                            }
                            string pollutant = reader.GetString(1);
                            double pollution = reader.GetDouble(2);
                            int pollutantId = -1;
                            using (MySqlCommand command = new MySqlCommand($"SELECT (ID) FROM pollutant WHERE pollutant_name = @pollutant_name", _connection))
                            {
                                command.Parameters.AddWithValue("@pollutant_name", pollutant);
                                using (MySqlDataReader sqlreader = command.ExecuteReader())
                                {
                                    if (sqlreader.Read())
                                    {
                                        pollutantId = sqlreader.GetInt32(0);
                                    }
                                    else
                                        System.Windows.MessageBox.Show($"Unknown pollutant: {pollutant}", "Error");
                                }
                            }
                            if (pollutantId != -1)
                            {
                                using (MySqlCommand addRecordCommand = new MySqlCommand("INSERT INTO records (object_id, pollutant_id, pollution_value, record_year) VALUES (@factoryId, @pollutantId, @pollution, @year);", _connection))
                                {
                                    addRecordCommand.Parameters.AddWithValue("@factoryId", factoryId);
                                    addRecordCommand.Parameters.AddWithValue("@pollutantId", pollutantId);
                                    addRecordCommand.Parameters.AddWithValue("@pollution", pollution);
                                    addRecordCommand.Parameters.AddWithValue("@year", year);

                                    addRecordCommand.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Growl.Error(ex.Message);
                return;
            }
        }

        public void GetFactories(ICollection<Factory> values)
        {
            using (MySqlCommand command = new MySqlCommand($"SELECT * FROM objects", _connection))
            {
                using (MySqlDataReader sqlreader = command.ExecuteReader())
                {
                    while (sqlreader.Read())
                    {
                        values.Add(new Factory() { Name = sqlreader.GetString(1), Desc = sqlreader.GetString(2), Lat = sqlreader.GetFloat(3), Lng = sqlreader.GetFloat(4) });
                    }
                }
            }
        }

        public void GetRecords(ICollection<Record> values, int year)
        {
            values.Clear();
            using (MySqlCommand command = new MySqlCommand("SELECT objects.object_name, objects.coord_lat, objects.coord_lng, pollutant.pollutant_name, pollutant.danger_class, pollutant.max_amount, records.pollution_value, records.record_year FROM records JOIN objects ON records.object_id = objects.ID JOIN pollutant ON records.pollutant_id = pollutant.ID WHERE records.record_year = @year ORDER BY objects.object_name;", _connection))
            {
                command.Parameters.AddWithValue("@year", year);
                using (MySqlDataReader sqlreader = command.ExecuteReader())
                {
                    while (sqlreader.Read())
                    {
                        values.Add(new Record()
                        {
                            Factory = new Factory() { Name = sqlreader.GetString(0), Lat = sqlreader.GetFloat(1), Lng = sqlreader.GetFloat(2) },
                            Pollutant = new Pollutant() { Name = sqlreader.GetString(3), DangerClass = sqlreader.GetInt32(4), MaxAmount = sqlreader.GetInt32(5) },
                            Pollution = sqlreader.GetFloat(6),
                            Year = sqlreader.GetInt32(7)
                        });
                    }
                }
            }
        }

        private MySqlConnection _connection = null;
    }
}
