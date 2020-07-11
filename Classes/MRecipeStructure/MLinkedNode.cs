using MSolvLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MRecipeStructure.Classes.MRecipeStructure
{
    [Serializable]
    public abstract class MLinkedNode : ViewModel, ICloneable
    {
        [XmlIgnore]
        public MLinkedNode Parent { get; set; }

        public MLinkedNode()
        {
            Parent = null;
        }

        protected MLinkedNode(MLinkedNode node)
        {
            Parent = node.Parent;
        }

        public abstract object Clone();
    }
}
