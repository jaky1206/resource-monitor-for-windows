using System;
using System.Configuration;
using System.Data.SQLite;
using System.Windows.Forms;

namespace WindowsResourceMonitor
{
    public partial class EntrepreneurIdInsertForm : Form
    {
        static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        //db
        static string dbPrefix = ConfigurationManager.AppSettings["dbPrefix"].ToString();
        static readonly string _appDirectoryName = "Entertech";
        static readonly string _dbFile = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\" + _appDirectoryName + @"\" + dbPrefix + "windows_resource_monitor.sqlite";
        static readonly string _dbConnectionString = "Data Source=" + _dbFile + ";Version=3;New=False;Compress=True;datetimeformat=CurrentCulture;";
        public EntrepreneurIdInsertForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(textBox1.Text.Trim() != "")
            {
                InsertEntrepreneurIdStore(textBox1.Text.Trim());
                this.Close();
            }
        }
        static void ExecuteNonQuery(string statement)
        {
            try
            {
                using (SQLiteConnection _dbConnection = new SQLiteConnection(_dbConnectionString))
                {
                    _dbConnection.Open();

                    using (var cmd = new SQLiteCommand(_dbConnection))
                    {
                        cmd.CommandText = statement;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
            }
        }
        static void InsertEntrepreneurIdStore(string entrepreneurId)
        {
            Guid Id = Guid.NewGuid();
            var statement = "INSERT INTO EntrepreneurIdStore (Id,EntrepreneurId) VALUES ('" + Id.ToString() + "','" + entrepreneurId + "')";
            ExecuteNonQuery(statement);
        }
    }
}
