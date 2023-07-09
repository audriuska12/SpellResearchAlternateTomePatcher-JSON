using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellResearchSynthesizer.Classes
{
    public class FormListContainer : IEnumerable<KeyValuePair<string, List<string>>>
    {
        protected Dictionary<string, List<string>> _items = new();

        private List<string> this[string key]
        {
            get
            {
                if (!_items.ContainsKey(key))
                {
                    _items[key] = new List<string>();
                }
                return _items[key];
            }
        }

        public void Add(string key, string form)
        {
            if (this[key].Contains(form))
            {
                throw new ArgumentException($"Form {form} duplicated for list {key}!");
            }
            this[key].Add(form);
        }

        public IEnumerator<KeyValuePair<string, List<string>>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, List<string>>>)_items).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_items).GetEnumerator();
        }
    }
}
