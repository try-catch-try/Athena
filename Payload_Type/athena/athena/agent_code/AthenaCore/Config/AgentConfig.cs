﻿using Agent.Interfaces;
namespace Agent.Config
{
    //Todo make this loadable via embedded resource json
    public class AgentConfig : IAgentConfig
    {
        public int chunk_size { get; set; } = 85000;
        public int inject { get; set; } = 2;
        public string? uuid
        {
            get
            {
                return _uuid;
            }
            set
            {
                _uuid = value;
                if (SetAgentConfigUpdated is not null)
                {
                    SetAgentConfigUpdated(this, new EventArgs());
                }
            }
        }
        private string? _uuid;
        public int sleep { get; set; } = 60;
        public int jitter { get; set; } = 10;
        private string? _psk;
        public string? psk
        {
            get
            {
                return _psk;
            }
            set
            {
                _psk = value;
                if (SetAgentConfigUpdated is not null)
                {
                    SetAgentConfigUpdated(this, new EventArgs());
                }
            }
        }
        public DateTime killDate { get; set; }
        public bool prettyOutput { get; set; }
        public bool debug { get; set; }
        public AgentConfig()
        {
            prettyOutput = true;
#if CHECKYMANDERDEV
            sleep = 1;
            jitter = 1;
            uuid = "596f3789-09e3-4611-b36e-7714306a21e4";
            psk = "gmNjeu2myPnz8+ZDSIwzcSuFNSy+EyPifinos+OM5Bw=";
            killDate = DateTime.Now.AddYears(1);
#else
            uuid = "%UUID%";
            psk = "%PSK%";
            int _tempInt = 0;
            if(int.TryParse("callback_interval", out _tempInt)){
                sleep = _tempInt;
            }
            if(int.TryParse("callback_jitter", out _tempInt))
            {
                jitter = _tempInt;
            }
            DateTime _killDate = DateTime.Now.AddYears(1);
            if(DateTime.TryParse("killdate", out _killDate))
            {
                killDate = _killDate;
            }
#endif
        }
        public event EventHandler? SetAgentConfigUpdated;
    }
}