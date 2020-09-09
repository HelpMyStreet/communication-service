using CommunicationService.Core.Domains;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.MessageService.Substitution
{
    public class TaskUpdateNewData : BaseDynamicData
    {
        public string One { get; private set; }
        public string Two { get; private set; }
        public string Three { get; private set; }
        public string Four { get; private set; }
        public string Five { get; private set; }
        public string Six { get; private set; }
        public string Seven { get; private set; }
        public string Eight { get; private set; }
        public string Nine { get; private set; }
        public string Ten { get; private set; }
        public string Eleven { get; private set; }
        public string Twelve { get; private set; }
        public string Thirteen { get; private set; }
        public bool ThirteenSupplied { get; private set; }
        public string Fourteen { get; private set; }

        public TaskUpdateNewData(
            string one,
            string two,
            string three,
            string four,
            string five,
            string six,
            string seven,
            string eight,
            string nine,
            string ten,
            string eleven,
            string twelve,
            string thirteen,
            bool thirteensupplied,
            string fourteen
            )
        {
            One = one;
            Two = two;
            Three = three;
            Four = four;
            Five = five;
            Six = six;
            Seven = seven;
            Eight = eight;
            Nine = nine;
            Ten = ten;
            Eleven = eleven;
            Twelve = twelve;
            Thirteen = thirteen;
            ThirteenSupplied = thirteensupplied;
            Fourteen = fourteen;
        }
    }
}