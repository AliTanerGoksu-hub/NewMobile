﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessSmartMobile.Models
{
    public class Balance
    {
        public string? tarih { get; set; }
        public string? islemTipi { get; set; }
        public double lAlacakTutar { get; set; }
        public double lBorcTutar { get; set; }
        public string? islemAciklama { get; set; }
        public double kalanBakiye { get; set; }
        public string? lFisNo { get; set; }
    }
}
