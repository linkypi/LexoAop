using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leox.Aop
{
    public class MAList : IEnumerable<MethodAspect> 
    {
        private List<MethodAspect> _list { get; set; }
        public MAList()
        {
            _list = new List<MethodAspect>();
        }

        public MethodAspect this[int index]
        {
            get
            {
                return _list[index];
            }
        }

        public void Add(MethodAspect item) {
            _list.Add(item);
            //默认是升序
            _list.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        public IEnumerator<MethodAspect> GetEnumerator()
        {
            return _list.GetEnumerator();
            //return new MAListEnumerator(_list);
        }


        IEnumerator<MethodAspect> IEnumerable<MethodAspect>.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
    }

}
