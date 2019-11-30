using System;
using System.Collections.Generic;
using System.Text;

namespace ApplicationCore.Entities
{
    public class InvoiceConfig : BaseEntity
    {
        public DayOfWeek Week { get; set; }
        public InvoiceWeekStateType WeekState { get; set; }
        public decimal TotalMinValue { get; set; }
        public decimal TotalMaxValue { get; set; }
        public int NrInvoices { get; set; }
        public DateTime? LastRunDate { get; set; }
    }
}
