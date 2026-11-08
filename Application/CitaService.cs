using Domain;
using Infrastructure;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application;

public class CitaService
{
    private readonly CitaRepository _citaRepository;
    private readonly HorarioRepository _horarioRepository; // 1. Declarar el campo privado

    // 2. Inyectar HorarioRepository en el constructor
    public CitaService(CitaRepository citaRepository, HorarioRepository horarioRepository)
    {
        _citaRepository = citaRepository;
        _horarioRepository = horarioRepository;
    }

    public async Task<int> AgendarCitaAsync(CrearCitaRequestDto req, int idPacienteToken)
    {
        var idPacienteFinal = req.IdPaciente ?? idPacienteToken;

        if (req.Fecha < DateOnly.FromDateTime(DateTime.Now))
        {
            throw new ArgumentException("No se pueden agendar citas en fechas pasadas.");
        }

        try
        {
            var idCita = await _citaRepository.CrearAsync(
                req.IdMedico, 
                idPacienteFinal, 
                req.Fecha, 
                req.HoraInicio
            );

            return idCita;
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            if (ex.ConstraintName != null && ex.ConstraintName.Contains("medico"))
            {
                throw new InvalidOperationException("Esta franja horaria acaba de ser ocupada por otro paciente. Por favor, elige otra.");
            }
            
            if (ex.ConstraintName != null && ex.ConstraintName.Contains("paciente"))
            {
                throw new InvalidOperationException("Ya tienes una cita programada exactamente a esta misma hora.");
            }

            throw new InvalidOperationException("Conflicto de horarios: la franja ya no está disponible.");
        }
    }

    public async Task<List<TimeSpan>> ObtenerFranjasDisponiblesAsync(int idMedico, DateOnly fecha)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        if (fecha < hoy)
        {
            return new List<TimeSpan>();
        }

        int idDia = fecha.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)fecha.DayOfWeek;

        // 3. Ahora _horarioRepository ya se encuentra disponible en el ámbito de la clase
        var horario = await _horarioRepository.ObtenerHorarioPorDiaAsync(idMedico, idDia);
        
        if (horario == null)
        {
            return new List<TimeSpan>();
        }

        var franjasPosibles = new List<TimeSpan>();
        var intervalo = TimeSpan.FromMinutes(horario.IntervaloMinutos);
        var horaActual = horario.HoraInicio;

        while (horaActual.Add(intervalo) <= horario.HoraFin)
        {
            if (fecha == hoy && horaActual < DateTime.Now.TimeOfDay)
            {
                horaActual = horaActual.Add(intervalo);
                continue;
            }

            franjasPosibles.Add(horaActual);
            horaActual = horaActual.Add(intervalo);
        }

        var horasOcupadas = await _citaRepository.ObtenerHorasOcupadasAsync(idMedico, fecha);
        var franjasDisponibles = franjasPosibles.Except(horasOcupadas).ToList();
        franjasDisponibles.Sort();

        return franjasDisponibles;
    }
}