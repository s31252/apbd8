﻿namespace Tutorial8.Models.DTOs;

public class TripDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int MaxPeople { get; set; }
    public List<CountryDTO> Countries { get; set; }
    public int RegisteredAt { get; set; }
    public int? PaymentDate { get; set; }
}

public class CountryDTO
{
    public string Name { get; set; }
}