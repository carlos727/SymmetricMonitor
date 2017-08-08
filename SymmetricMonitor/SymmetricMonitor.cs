using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.ServiceProcess;
using System.Runtime.InteropServices;
using SymmetricMonitor.Core;
using Redsis.EVA.Client.Common.Telemetria;

namespace SymmetricMonitor
{
    public partial class SymmetricMonitor : ServiceBase
    {
        static readonly log4net.ILog log = log4net.LogManager
            .GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private int _iConnectivity;
        private int _iPendingUpdate;
        private int _iAlerts;
        private int _iPendingBatch;
        private int _iSumBytes;
        private string _channels;
        private string _customerId;
        private string _sharedKey;
        private string _clientCompany;
        private ConnectionProvider _conn;
        private CanalAzure _canal;

        public SymmetricMonitor()
        {
            InitializeComponent();

            var appSettings = ConfigurationManager.AppSettings;
            _iConnectivity = Int32.Parse(appSettings["Connectivity"]);
            _iPendingUpdate = Int32.Parse(appSettings["ChannelUpdateStatus"]);
            _iAlerts = Int32.Parse(appSettings["Alerts"]);
            _iPendingBatch = Int32.Parse(appSettings["PendingBatch"]);
            _iSumBytes = Int32.Parse(appSettings["SumBytes"]);
            _channels = appSettings["Channels"];
            _customerId = appSettings["customerId"];
            _sharedKey = appSettings["sharedKey"];
            _clientCompany = appSettings["clientCompany"];

            _conn = new ConnectionProvider();
            _canal = new CanalAzure(_customerId, _sharedKey);

            Telemetria.Instancia
                .Tipo("SymmetricDS")
                .Empresa(_clientCompany)
                .Programa("SymMonitor")
                .VersionLog("1.0")
                .AgregaCanal(_canal);
        }

        protected override void OnStart(string[] args)
        {
            // Log
            log.Info("Starting SymmetricDS Monitor Service v1.0.0.0");
            log.Info("SymMonitor request every x seconds to the database to get information about nodes");

            //
            Connectivity();
            ChannelUpdateStatus();
            Alerts();
            PendingBatch();
            SumBytes();

            // Set up timers to trigger every x seconds.  
            System.Timers.Timer cTimer = new System.Timers.Timer();
            cTimer.Interval = _iConnectivity * 1000;
            cTimer.Elapsed += new System.Timers.ElapsedEventHandler(ConnectivityTimer);
            cTimer.Start();

            System.Timers.Timer uTimer = new System.Timers.Timer();
            uTimer.Interval = _iPendingUpdate * 1000;
            uTimer.Elapsed += new System.Timers.ElapsedEventHandler(ChannelUpdateStatusTimer);
            uTimer.Start();

            System.Timers.Timer aTimer = new System.Timers.Timer();
            aTimer.Interval = _iAlerts * 1000;
            aTimer.Elapsed += new System.Timers.ElapsedEventHandler(AlertsTimer);
            aTimer.Start();

            System.Timers.Timer pTimer = new System.Timers.Timer();
            pTimer.Interval = _iPendingBatch * 1000;
            pTimer.Elapsed += new System.Timers.ElapsedEventHandler(PendingBatchTimer);
            pTimer.Start();

            System.Timers.Timer bTimer = new System.Timers.Timer();
            bTimer.Interval = _iSumBytes * 1000;
            bTimer.Elapsed += new System.Timers.ElapsedEventHandler(SumBytesTimer);
            bTimer.Start();
        }

        protected override void OnStop()
        {
            log.Info("Stopping service");
        }

        private void ConnectivityTimer(object sender, System.Timers.ElapsedEventArgs args)
        {
            Connectivity();
        }

        private void ChannelUpdateStatusTimer(object sender, System.Timers.ElapsedEventArgs args)
        {
            ChannelUpdateStatus();
        }

        private void AlertsTimer(object sender, System.Timers.ElapsedEventArgs args)
        {
            Alerts();
        }

        private void PendingBatchTimer(object sender, System.Timers.ElapsedEventArgs args)
        {
            PendingBatch();
        }

        private void SumBytesTimer(object sender, System.Timers.ElapsedEventArgs args)
        {
            SumBytes();
        }

        public void Connectivity()
        {
            try
            {
                string sql =
                    "SELECT cod_localidad, descrip, fecha_apertura, batch_id, channel_id, status, byte_count, sql_state, sql_code, sql_message, last_update_time " +
                    "FROM sym_node LEFT JOIN (" +
                        "SELECT node_id, batch_id, channel_id, status, byte_count, sql_state, sql_code, sql_message, last_update_time, " +
                        "ROW_NUMBER() OVER(PARTITION BY sym_incoming_batch.node_id ORDER BY last_update_time DESC) AS RN " +
                        "FROM sym_incoming_batch)  AS temp ON sym_node.node_id = temp.node_id " +
                        "LEFT JOIN localidad ON (cod_localidad = RIGHT(sym_node.node_id, 3)) " +
                    "WHERE (rn = 1 or rn is null) and node_group_id not like 'lab%' and node_group_id not like 'central' and cod_localidad is not null";

                var batches = _conn.Query<Batch>(sql);

                int countNodes = batches.Count();
                int nodesConnected = 0;
                int nodesDisconnected = 0;

                var eventList = new List<Evento>();
                var metricaSimpleList = new List<MetricaSimple>();

                batches.ToList().ForEach(batch =>
                {

                    string nodeLabel = "Disconnected";
                    string update = "1900-01-01T00:00:00";

                    if (batch.last_update_time.HasValue)
                    {
                        update = batch.last_update_time.Value.ToString("yyyy-mm-ddThh:mm:ss");
                        double diffDate = (DateTime.Now - batch.last_update_time.Value).TotalHours;

                        if (diffDate > 1)
                        {
                            nodesDisconnected++;
                        }
                        else
                        {
                            nodesConnected++;
                            nodeLabel = "Connected";
                        }
                    }

                    string opening = batch.fecha_apertura.HasValue ? batch.fecha_apertura.Value.ToString("yyyy-mm-dd") : "1900-01-01";

                    var eventItem = new Evento("lastChecking")
                        .AgregarPropiedad("Code", batch.cod_localidad)
                        .AgregarPropiedad("Description", batch.descripFormatted())
                        .AgregarPropiedad("DateOpening", opening)
                        .AgregarPropiedad("Channel", batch.channel_id)
                        .AgregarPropiedad("BatchStatus", batch.status)
                        .AgregarPropiedad("LastUpdate", update)
                        .AgregarPropiedad("NodeStatus", nodeLabel);

                    eventList.Add(eventItem);
                });

                metricaSimpleList.Add(new MetricaSimple("TotalServers", countNodes));
                metricaSimpleList.Add(new MetricaSimple("ServersConnected", nodesConnected));
                metricaSimpleList.Add(new MetricaSimple("ServersDisconnected", nodesDisconnected));

                Telemetria.Instancia.Id("symConnectivity").AgregaMetricas(eventList);
                Telemetria.Instancia.Id("symStatistic").AgregaMetricas(metricaSimpleList);

                log.Info(string.Format("Checking last connection: {0} Eventos enviados.", eventList.Count));
            }
            catch (Exception ex)
            {
                log.Error("Connectivity: " + ex.Message);
                Telemetria.Instancia.Id("symException").AgregaMetrica(new Excepcion(ex).AgregarPropiedad("Method", "Connectivity"));
            }
        }

        public void ChannelUpdateStatus()
        {
            var channels = _channels.Split(',');
            channels.ToList().ForEach(channel =>
            {
                try
                {
                    string sql =
                    "SELECT cod_localidad, descrip, fecha_apertura, batch_id, channel_id, status, byte_count, sql_state, sql_code, sql_message, last_update_time " +
                    "FROM sym_node LEFT JOIN (" +
                        "SELECT node_id, batch_id, channel_id, status, byte_count, sql_state, sql_code, sql_message, last_update_time, " +
                        "ROW_NUMBER() OVER(PARTITION BY sym_outgoing_batch.node_id ORDER BY last_update_time DESC) AS RN " +
                        "FROM sym_outgoing_batch WHERE channel_id = '" + channel + "') AS temp ON sym_node.node_id = temp.node_id " +
                        "LEFT JOIN localidad ON (cod_localidad = RIGHT(sym_node.node_id, 3))" +
                    "WHERE (rn = 1 or rn is null) and node_group_id not like 'lab%' and node_group_id not like 'central' and cod_localidad is not null";

                    var batches = _conn.Query<Batch>(sql);

                    string tChannel = "";
                    if (channel.Contains('_'))
                    {
                        var channel_parts = channel.Split('_');
                        tChannel = channel_parts[0].First().ToString().ToUpper() + channel_parts[0].Substring(1);
                        for (int i = 1; i < channel_parts.Length; i++)
                        {
                            tChannel += channel_parts[i].First().ToString().ToUpper() + channel_parts[i].Substring(1);
                        }
                    }
                    else
                    {
                        tChannel += channel.First().ToString().ToUpper() + channel.Substring(1);
                    }

                    var countStatus = new Dictionary<string, int>
                    {
                        { "RE", 0 },
                        { "NE", 0 },
                        { "QE", 0 },
                        { "SE", 0 },
                        { "LD", 0 },
                        { "OK", 0 },
                        { "ER", 0 }
                    };

                    var eventList = new List<Evento>();
                    var metricaSimpleList = new List<MetricaSimple>();

                    batches.ToList().ForEach(batch =>
                    {
                        if (countStatus.Keys.Contains(batch.status))
                        {
                            countStatus[batch.status] += 1;
                        } else
                        {
                            countStatus["OK"] += 1;
                        }

                        string opening = batch.fecha_apertura.HasValue ? batch.fecha_apertura.Value.ToString("yyyy-mm-dd") : "1900-01-01";
                        string update = "1900-01-01T00:00:00";
                        if (batch.last_update_time.HasValue)
                        {
                            update = batch.last_update_time.Value.ToString("yyyy-mm-ddThh:mm:ss");
                        }

                        var eventItem = new Evento(tChannel)
                            .AgregarPropiedad("Code", batch.cod_localidad)
                            .AgregarPropiedad("Description", batch.descripFormatted())
                            .AgregarPropiedad("DateOpening", opening)
                            .AgregarPropiedad("BatchID", batch.batch_id)
                            .AgregarPropiedad("Channel", batch.channel_id)
                            .AgregarPropiedad("Status", batch.status)
                            .AgregarPropiedad("SqlCode", batch.sql_code)
                            .AgregarPropiedad("SqlStatus", batch.sql_state)
                            .AgregarPropiedad("SqlMessage", batch.sql_message)
                            .AgregarPropiedad("LastUpdate", update);

                        eventList.Add(eventItem);
                    });

                    //log.Info("Total batches: " + batches.Count());
                    //log.Info("Total batches with RE status: " + countStatus["RE"]);
                    //log.Info("Total batches with NE status: " + countStatus["NE"]);
                    //log.Info("Total batches with QE status: " + countStatus["QE"]);
                    //log.Info("Total batches with SE status: " + countStatus["SE"]);
                    //log.Info("Total batches with LD status: " + countStatus["LD"]);
                    //log.Info("Total batches with OK status: " + countStatus["OK"]);
                    //log.Info("Total batches with ER status: " + countStatus["ER"]);

                    metricaSimpleList.Add(new MetricaSimple("TotalBatches", batches.Count()));
                    metricaSimpleList.Add(new MetricaSimple("StatusRE", countStatus["RE"]));
                    metricaSimpleList.Add(new MetricaSimple("StatusNE", countStatus["NE"]));
                    metricaSimpleList.Add(new MetricaSimple("StatusQE", countStatus["QE"]));
                    metricaSimpleList.Add(new MetricaSimple("StatusSE", countStatus["SE"]));
                    metricaSimpleList.Add(new MetricaSimple("StatusLD", countStatus["LD"]));
                    metricaSimpleList.Add(new MetricaSimple("StatusOK", countStatus["OK"]));
                    metricaSimpleList.Add(new MetricaSimple("StatusER", countStatus["ER"]));

                    metricaSimpleList.ForEach(
                        metrica => { metrica.AgregarPropiedad("Channel", tChannel); }
                    );

                    Telemetria.Instancia.Id("symUpdate").AgregaMetricas(eventList);
                    Telemetria.Instancia.Id("symStatistic").AgregaMetricas(metricaSimpleList);

                    log.Info(string.Format("Checking {0} channel updates: {1} Eventos enviados.", channel, eventList.Count));
                }
                catch (Exception ex)
                {
                    log.Error(string.Format("ChannelUpdateStatus - {0}: {1}", channel, ex.Message));
                    Telemetria.Instancia.Id("symException").AgregaMetrica(new Excepcion(ex)
                        .AgregarPropiedad("Method", "ChannelUpdateStatus")
                        .AgregarPropiedad("Channel", channel));
                }
            });
        }

        public void Alerts()
        {
            GetErrors("sym_outgoing_batch");
            GetErrors("sym_incoming_batch");
        }

        public void PendingBatch()
        {
            try
            {
                string sql =
                    "SELECT cod_localidad, descrip, fecha_apertura, batch_id, channel_id, status, byte_count, sql_state, sql_code, sql_message, last_update_time " +
                    "FROM sym_outgoing_batch LEFT JOIN localidad ON (cod_localidad = RIGHT(node_id, 3)) " +
                    "WHERE status='NE' and cod_localidad is not null";

                var batches = _conn.Query<Batch>(sql);
                var eventList = new List<Evento>();

                batches.ToList().ForEach(batch =>
                {
                    string opening = batch.fecha_apertura.HasValue ? batch.fecha_apertura.Value.ToString("yyyy-mm-dd") : "1900-01-01";
                    string update = "1900-01-01T00:00:00";
                    if (batch.last_update_time.HasValue)
                    {
                        update = batch.last_update_time.Value.ToString("yyyy-mm-ddThh:mm:ss");
                    }

                    var eventItem = new Evento("PendingBatch")
                            .AgregarPropiedad("Code", batch.cod_localidad)
                            .AgregarPropiedad("Description", batch.descripFormatted())
                            .AgregarPropiedad("DateOpening", opening)
                            .AgregarPropiedad("BatchID", batch.batch_id)
                            .AgregarPropiedad("Channel", batch.channel_id)
                            .AgregarPropiedad("Status", batch.status)
                            .AgregarPropiedad("SqlCode", batch.sql_code)
                            .AgregarPropiedad("SqlStatus", batch.sql_state)
                            .AgregarPropiedad("SqlMessage", batch.sql_message)
                            .AgregarPropiedad("LastUpdate", update);

                    eventList.Add(eventItem);
                });

                Telemetria.Instancia.Id("symStatistic").AgregaMetrica(new MetricaSimple("PendingBatches", batches.Count()));
                Telemetria.Instancia.Id("symPending").AgregaMetricas(eventList);

                log.Info(string.Format("Checking pending batches: {0} Eventos enviados.", eventList.Count));
            }
            catch (Exception ex)
            {
                log.Error("PendingBatch: " + ex.Message);
                Telemetria.Instancia.Id("symException").AgregaMetrica(new Excepcion(ex).AgregarPropiedad("Method", "PendingBatch"));
            }
        }

        public void SumBytes()
        {
            GetBytes("sym_outgoing_batch");
            GetBytes("sym_incoming_batch");
        }

        internal void GetErrors(string table)
        {
            string orientation = table.Equals("sym_outgoing_batch") ? "Outgoing" : "Incoming";
            //log.Info("# " + orientation + " #");

            try
            {
                string sql =
                    "SELECT cod_localidad, descrip, fecha_apertura, batch_id, channel_id, status, byte_count, sql_state, sql_code, sql_message, last_update_time " +
                    "FROM " + table + " LEFT JOIN localidad ON (cod_localidad = RIGHT(node_id, 3)) " +
                    "WHERE error_flag=1 and cod_localidad is not null";

                var batches = _conn.Query<Batch>(sql);

                int countErrors = batches.Count();
                int errors1H = 0;
                int errors24H = 0;
                int errors48H = 0;
                int errorsMax = 0;

                var eventList = new List<Evento>();
                var metricaSimpleList = new List<MetricaSimple>();

                batches.ToList().ForEach(batch =>
                {
                    if (batch.last_update_time.HasValue)
                    {
                        double diffDate = (DateTime.Now - batch.last_update_time.Value).TotalHours;
                        string nodeLabel = "";

                        if (diffDate <= 1)
                        {
                            errors1H++;
                            nodeLabel = "1HOUR";
                        }
                        else
                        {
                            if (diffDate <= 24)
                            {
                                errors24H++;
                                nodeLabel = "24HOURS";
                            }
                            else
                            {
                                if (diffDate <= 48)
                                {
                                    errors48H++;
                                    nodeLabel = "48HOURS";
                                }
                                else
                                {
                                    errorsMax++;
                                    nodeLabel = "MaxHOURS";
                                }
                            }
                        }

                        string opening = batch.fecha_apertura.HasValue ? batch.fecha_apertura.Value.ToString("yyyy-mm-dd") : "null";

                        var eventItem = new Evento("Alert")
                                .AgregarPropiedad("Code", batch.cod_localidad)
                                .AgregarPropiedad("Description", batch.descripFormatted())
                                .AgregarPropiedad("DateOpening", opening)
                                .AgregarPropiedad("BatchID", batch.batch_id)
                                .AgregarPropiedad("Channel", batch.channel_id)
                                .AgregarPropiedad("Status", batch.status)
                                .AgregarPropiedad("SqlCode", batch.sql_code)
                                .AgregarPropiedad("SqlStatus", batch.sql_state)
                                .AgregarPropiedad("SqlMessage", batch.sql_message)
                                .AgregarPropiedad("LastUpdate", batch.last_update_time.Value.ToString("yyyy-mm-ddThh:mm:ss"))
                                .AgregarPropiedad("Rank", nodeLabel)
                                .AgregarPropiedad("Orientation", orientation);

                        eventList.Add(eventItem);
                    }
                });

                //log.Info("Total alerts: " + countErrors);
                //log.Info("Alerts last hour: " + errors1H);
                //log.Info("Alerts last 24 hours: " + errors24H);
                //log.Info("Alerts last 48 hours: " + errors48H);
                //log.Info("Alerts highest 48 hours: " + errorsMax);

                metricaSimpleList.Add(new MetricaSimple("TotalAlerts", countErrors).AgregarPropiedad("Orientation", orientation));
                metricaSimpleList.Add(new MetricaSimple("AlertsRank1", errors1H).AgregarPropiedad("Orientation", orientation));
                metricaSimpleList.Add(new MetricaSimple("AlertsRank24", errors24H).AgregarPropiedad("Orientation", orientation));
                metricaSimpleList.Add(new MetricaSimple("AlertsRank48", errors48H).AgregarPropiedad("Orientation", orientation));
                metricaSimpleList.Add(new MetricaSimple("AlertsRankMax", errorsMax).AgregarPropiedad("Orientation", orientation));

                Telemetria.Instancia.Id("symAlert").AgregaMetricas(eventList);
                Telemetria.Instancia.Id("symStatistic").AgregaMetricas(metricaSimpleList);

                log.Info(string.Format("Checking {0} alerts: {1} Eventos enviados.", orientation, eventList.Count));
            }
            catch (Exception ex)
            {
                log.Error("Alerts: " + ex.Message);
                Telemetria.Instancia.Id("symException").AgregaMetrica(new Excepcion(ex)
                    .AgregarPropiedad("Method", "Alerts")
                    .AgregarPropiedad("Orientation", orientation));
            }
        }

        internal void GetBytes(string table)
        {
            string orientation = table.Equals("sym_outgoing_batch") ? "Outgoing" : "Incoming";

            try
            {
                string sql = "SELECT SUM(byte_count) AS sum FROM " + table + " WHERE last_update_time BETWEEN DATEADD(HOUR,-1,GETDATE()) and GETDATE()";

                var bytes = _conn.Query<Core.Byte>(sql);
                var sumBytes = bytes.FirstOrDefault().sum;

                Telemetria.Instancia.Id("symStatistic")
                    .AgregaMetrica(new MetricaSimple("SumBytes", sumBytes).AgregarPropiedad("Orientation", orientation));

                log.Info(string.Format("Checking {0} bytes: {1}", orientation, sumBytes));
            }
            catch (Exception ex)
            {
                log.Error("SumBytes: " + ex.Message);
                Telemetria.Instancia.Id("symException").AgregaMetrica(new Excepcion(ex)
                    .AgregarPropiedad("Method", "SumBytes")
                    .AgregarPropiedad("Orientation", orientation));
            }
        }
    }
}