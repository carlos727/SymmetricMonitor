using System;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;
using System.Collections;
using System.Threading;

namespace Redsis.EVA.Client.Common.Telemetria
{
    /// <summary>
    /// Se requiere inicializar Programa, Version, Empresa, Usuario (opcional).
    /// 
    /// El método ActivaPing(int minutos) activa función de evento "ping"
    /// hacia los canales según los minutos de inactividad pasados como parámetro.
    /// El "ping" es enviado si durantes los minutos del parámetro no se ha 
    /// remitido ningún mensaje (objeto metrica). Dado que la verificación de 
    /// transferencia de mensajes de hace cada n minutos (parámetro),
    /// el primer "ping" luego de la inactividad puede ser de 2n minutos aproximadamente, 
    /// luego de esto, los "ping" subsiguientes, mientras no se tengan mensajes, 
    /// serán cada n minutos.
    /// </summary>
    public class Telemetria
    {
        static readonly log4net.ILog log = log4net.LogManager
            .GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly int MAX_GRADO_PARALELISMO = 3;
        private static readonly int MAX_ITEMS_EN_COLA = 1000;
        private static readonly int MINUTOS_PING = 5;
        // Singleton
        private static readonly Telemetria _telemetria = new Telemetria();
        private List<ICanal> _canales = new List<ICanal>();
        private ActionBlock<ICmd> Procesador = null;
        private bool _inicializado = false;
        // Instrumentación
        private long _maxLongitudCola = 0;
        // Control de ping
        private int _minutosPing = MINUTOS_PING;
        private bool _pingActivo = false;
        private long _nroPing = 1;
        private DateTime _tiempoUltimoMensaje = DateTime.Now;
        private Timer _temporizadorPing = null;
        // Campos de log.
        private string _nombreLog = null;
        private string _nombrePrograma = null;
        private string _versionLog = "1.0";
        private string _empresa = null;
        private string _id = null;
        private string _usuario = "";

        // Singleton
        private Telemetria() { }

        public static Telemetria Instancia { get { return _telemetria; } }

        public Telemetria Tipo(string nombreLog)
        {
            _nombreLog = nombreLog;
            return Instancia;
        }
        public Telemetria Programa(string nombrePrograma)
        {
            _nombrePrograma = nombrePrograma;
            return Instancia;
        }

        public Telemetria VersionLog(string version)
        {
            _versionLog = version;
            return Instancia;
        }

        public Telemetria Empresa(string empresa)
        {
            _empresa = empresa;
            return Instancia;
        }

        public Telemetria Id(string id)
        {
            _id = id;
            return Instancia;
        }

        public Telemetria Usuario(string usuario)
        {
            _usuario = usuario;
            return Instancia;
        }

        public Telemetria ActivaPing()
        {
            _pingActivo = true;
            ActivaTimerPing();
            return Instancia;
        }

        public Telemetria ActivaPing(int minutos)
        {
            if (minutos <= 0)
                throw new ArgumentOutOfRangeException("Tiempo ping debe ser positivo.");
            _minutosPing = minutos;
            ActivaPing();
            return Instancia;
        }

        public bool PingActivo { get {return _pingActivo; } }
        public int MinutosPing { get { return _minutosPing; } }
        public DateTime TiempoUltimoMensaje { get { return _tiempoUltimoMensaje; } }
        public long NroPings { get { return _nroPing; } }
        public long IncrementaNroPings()
        {
           return  _nroPing++;
        }

        private void ActivaTimerPing()
        {
            _tiempoUltimoMensaje = DateTime.Now;
            var ping = new EventoPing(_minutosPing);
            _temporizadorPing = new Timer(ping.ProcesaTemporizador, null, 0, _minutosPing * 60 * 1000);
        }

        public Telemetria AgregaCanal(ICanal canal)
        {
            _canales.Add(canal);
            return Instancia;
        }

        private void Inicializar()
        {
            if (_canales.Count == 0)
                throw new ArgumentException("canales no definidos.");
            if (string.IsNullOrWhiteSpace(_nombreLog))
                throw new ArgumentException("NombreLog no definida.");
            if (string.IsNullOrWhiteSpace(_empresa))
                throw new ArgumentException("Empresa no definida.");
            if (string.IsNullOrWhiteSpace(_id))
                throw new ArgumentException("Id no definido.");
            if (string.IsNullOrWhiteSpace(_nombrePrograma))
                throw new ArgumentException("Programa no definido.");
            if (string.IsNullOrWhiteSpace(_versionLog))
                throw new ArgumentException("Versión log no definida.");

            int paralelismo = _canales.Count <= MAX_GRADO_PARALELISMO ? _canales.Count : MAX_GRADO_PARALELISMO;

            Procesador = new ActionBlock<ICmd>(ejecutor =>
            {
                ejecutor.Procesar();
            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = paralelismo });
            _inicializado = true;
        }

        private void Encola(Object metrica)
        {
            if (!_inicializado)
                Inicializar();

            if (Procesador.InputCount > MAX_ITEMS_EN_COLA)
            {
                // Para evitar overflow, no se agregan mas items a la cola, segun
                // tope superior definido.
                log.Warn(string.Format("Mensaje descartado. Items en cola telemetría {0}, máximo permitido {1}.",
                    Procesador.InputCount,
                    MAX_ITEMS_EN_COLA));
                return;
            }
            _tiempoUltimoMensaje = DateTime.Now;
            //Serilog.Log.Debug("_tiempoUltimoMensaje: " + _tiempoUltimoMensaje);
            foreach (ICanal c in _canales)
            {
                if (metrica is Metrica)
                {
                    Procesador.Post(new CmdJson(c, _nombreLog, metrica as Metrica));
                }
                else if (metrica is IList)
                {
                    Procesador.Post(new CmdJsonLista(c, _nombreLog, metrica as IList));
                }
                else
                {
                    throw new ArgumentException("Tipo de dato errado: " + metrica.GetType());
                }

            }
            long itemsEnCola = Procesador.InputCount;
            if (itemsEnCola > _maxLongitudCola)
                _maxLongitudCola = itemsEnCola;
        }

        private void CompletarMetrica(Metrica metrica)
        {
            metrica.Preparar();
            metrica.TimeStamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffff");
            metrica.Programa = _nombrePrograma;
            metrica.Version = metrica.Version;
            metrica.Nivel = metrica.Nivel;
            metrica.Empresa = _empresa;
            metrica.Id = _id;
            metrica.Usuario = _usuario;
        }

        public void AgregaMetrica(Metrica metrica)
        {
            CompletarMetrica(metrica);
            Encola(metrica);
        }

        public void AgregaMetricas(List<MetricaSimple> metricas)
        {
            foreach (Metrica m in metricas)
            {
                CompletarMetrica(m);
            }
            Encola(metricas);
        }

        public void AgregaMetricas(List<MetricaTemporizador> metricas)
        {
            foreach (Metrica m in metricas)
            {
                CompletarMetrica(m);
            }
            Encola(metricas);
        }

        public void AgregaMetricas(List<MetricaAgregada> metricas)
        {
            foreach (Metrica m in metricas)
            {
                CompletarMetrica(m);
            }
            Encola(metricas);
        }

        public void AgregaMetricas(List<Evento> metricas)
        {
            foreach (Metrica m in metricas)
            {
                CompletarMetrica(m);
            }
            Encola(metricas);
        }
    }
}
