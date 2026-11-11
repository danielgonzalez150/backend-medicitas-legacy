using Domain.DTOs;
using Infrastructure;
using Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services;

public class HorarioService
{
    private readonly HorarioRepository _repo;

    public HorarioService(HorarioRepository repo)
    {
        _repo = repo;
    }

    public async Task<int> ConfigurarHorarioAsync(int idMedico, ConfigurarHorarioRequestDto dto)
    {
        if (dto.HoraInicio >= dto.HoraFin)
            throw new ArgumentException("La hora de inicio debe ser anterior a la hora de fin.");

        if (dto.IntervaloMinutos <= 0)
            throw new ArgumentException("El intervalo en minutos debe ser mayor a cero.");

        return await _repo.GuardarHorarioAsync(idMedico, dto);
    }

    public async Task<IEnumerable<HorarioResponseDto>> ObtenerHorariosPorMedicoAsync(int idMedico)
    {
        return await _repo.ObtenerPorMedicoAsync(idMedico);
    }

    public async Task<IEnumerable<TimeOnly>> ObtenerFranjasDisponiblesAsync(int idMedico, DateOnly fecha)
    {
        int idDia = (int)fecha.DayOfWeek == 0 ? 7 : (int)fecha.DayOfWeek;

        var horario = await _repo.ObtenerHorarioPorDiaAsync(idMedico, idDia);
        
        if (horario == null || !horario.Estado)
            return new List<TimeOnly>(); 

        var horasOcupadas = await _repo.ObtenerHorasOcupadasAsync(idMedico, fecha);

        var franjasDisponibles = new List<TimeOnly>();
        var franjaActual = horario.HoraInicio;

        while (franjaActual.AddMinutes(horario.IntervaloMinutos) <= horario.HoraFin)
        {
            if (!horasOcupadas.Contains(franjaActual))
            {
                franjasDisponibles.Add(franjaActual);
            }
            franjaActual = franjaActual.AddMinutes(horario.IntervaloMinutos);
        }

        return franjasDisponibles;
    }
}