//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Models.Ado
{
    using System;
    using System.Collections.Generic;
    
    public partial class soruBurc
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public soruBurc()
        {
            this.usersDetail = new HashSet<usersDetail>();
        }
    
        public byte recid { get; set; }
        public string tanim { get; set; }
        public Nullable<byte> value { get; set; }
        public string dil { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<usersDetail> usersDetail { get; set; }
    }
}
