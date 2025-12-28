using System;
using System.Collections.Generic;
using System.Text;

namespace Labs.Domain.Models
{
    public class ListModel<T>
    {
        // запрошенный список объектов
        public List<T> Items { get; set; } = new();
        // номер текущей страницы
        public int CurrentPage { get; set; } = 1;
        // общее кол-во страниц
        public int TotalPages { get; set; } = 1;
    }
}
