using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ApplicationCore.Entities
{
    public class InvoiceConfig : BaseEntity
    {
        [Display(Name = "Dia da Semana")]
        public DayOfWeek Week { get; set; }
        [Display(Name = "Ativo")]
        public bool IsActive { get; set; }
        [Display(Name = "Máximo de valor por Fatura")]
        public decimal MaxValue { get; set; }
        [Display(Name = "Nº de Faturas")]
        public int NrInvoices { get; set; }
        public DateTime? LastRunDate { get; set; }
    }
}
