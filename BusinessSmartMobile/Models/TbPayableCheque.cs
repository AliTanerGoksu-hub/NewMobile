﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessSmartMobile.Models
{
    public class TbPayableCheque
    {
        public string sVerenFirmaAciklama { get; set; }
        public string lCekSenetNo { get; set; }
        public double lKalan { get; set; }
        public double lDovizMiktari1 { get; set; }
        public string dteVadeTarihi { get; set; }
        public string sAlanFirmaAciklama { get; set; }
        public string sBankaAciklama { get; set; }
    }
}
