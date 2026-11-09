using Domain.DTOs;
using Infrastructure;
using Infrastructure.Repositories;
using System;
using System.Collections.Generic;
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

    // Lógica principal: Generar franjas disponibles para que un paciente agende
    public async Task<IEnumerable<TimeSpan>> ObtenerFranjasDisponiblesAsync(int idMedico, DateOnly fecha)
    {
        // 1. Determinar el día de la semana (En tu BD: 1=Lunes, 7=Domingo)
        int idDia = (int)fecha.DayOfWeek == 0 ? 7 : (int)fecha.DayOfWeek;

        // 2. Traer la configuración de trabajo del médico para ese día
        var horario = await _repo.ObtenerHorarioPorDiaAsync(idMedico, idDia);
        
        // Si no hay configuración o está inactivo, retorna lista vacía
        if (horario == null || !horario.Estado)
            return new List<TimeSpan>(); 

        // 3. Traer las horas que ya están ocupadas por otras citas en esa misma fecha
        var horasOcupadas = await _repo.ObtenerHorasOcupadasAsync(idMedico, fecha);

        // 4. Generar intervalos y restar los ocupados
        var franjasDisponibles = new List<TimeSpan>();
        var franjaActual = horario.HoraInicio;

        while (franjaActual.Add(TimeSpan.FromMinutes(horario.IntervaloMinutos)) <= horario.HoraFin)
        {
            if (!horasOcupadas.Contains(franjaActual))
            {
                franjasDisponibles.Add(franjaActual);
            }
            // Avanzar a la siguiente franja (Ej: de 08:00 a 08:30)
            franjaActual = franjaActual.Add(TimeSpan.FromMinutes(horario.IntervaloMinutos));
        }

        return franjasDisponibles;
    }
}