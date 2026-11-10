using Domain.DTOs;
using Infrastructure;
using Infrastructure.Repositories;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services;

public class CitaService
{
    private readonly CitaRepository _citaRepository;
    private readonly HorarioRepository _horarioRepository;

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
            // CORRECCIÓN: Se agrega req.IdSede según el nuevo esquema del repositorio
            var idCita = await _citaRepository.CrearAsync(
                req.IdMedico, 
                idPacienteFinal, 
                req.Fecha, 
                req.HoraInicio,
                req.IdSede 
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

    // MODIFICADO: Ahora evalúa la disponibilidad en un rango de fechas y devuelve un Diccionario
    public async Task<Dictionary<DateOnly, List<TimeSpan>>> ObtenerFranjasDisponiblesAsync(int idMedico, DateOnly fechaInicio, DateOnly fechaFin)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        
        // Ajustamos la fecha de inicio si intentan buscar en el pasado
        if (fechaInicio < hoy) fechaInicio = hoy;
        
        if (fechaInicio > fechaFin)
        {
            return new Dictionary<DateOnly, List<TimeSpan>>();
        }

        // Consulta optimizada para traer todas las horas ocupadas del rango de una sola vez
        var ocupadasEnRango = await _citaRepository.ObtenerHorasOcupadasEnRangoAsync(idMedico, fechaInicio, fechaFin);
        
        var disponibilidad = new Dictionary<DateOnly, List<TimeSpan>>();

        // Iteramos sobre cada día del rango
        for (var fecha = fechaInicio; fecha <= fechaFin; fecha = fecha.AddDays(1))
        {
            int idDia = fecha.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)fecha.DayOfWeek;

            var horario = await _horarioRepository.ObtenerHorarioPorDiaAsync(idMedico, idDia);
            
            if (horario == null)
            {
                disponibilidad[fecha] = new List<TimeSpan>(); // No trabaja este día
                continue;
            }

            var franjasPosibles = new List<TimeSpan>();
            var intervalo = TimeSpan.FromMinutes(horario.IntervaloMinutos);
            var horaActual = horario.HoraInicio;

            while (horaActual.Add(intervalo) <= horario.HoraFin)
            {
                // Si es hoy, omitimos las horas que ya pasaron
                if (fecha == hoy && horaActual < DateTime.Now.TimeOfDay)
                {
                    horaActual = horaActual.Add(intervalo);
                    continue;
                }

                franjasPosibles.Add(horaActual);
                horaActual = horaActual.Add(intervalo);
            }

            // Filtramos las horas ocupadas específicamente para el día en curso de la iteración
            var horasOcupadasDelDia = ocupadasEnRango
                .Where(o => o.Fecha == fecha)
                .Select(o => o.HoraInicio)
                .ToList();

            var franjasDisponibles = franjasPosibles.Except(horasOcupadasDelDia).ToList();
            franjasDisponibles.Sort();

            disponibilidad[fecha] = franjasDisponibles;
        }

        return disponibilidad;
    }

    public async Task<IEnumerable<CitaDetalleDto>> ObtenerAgendaPacienteAsync(int idPaciente, DateOnly fechaInicio, DateOnly fechaFin)
    {
        if (fechaInicio > fechaFin)
        {
            throw new ArgumentException("La fecha de inicio no puede ser posterior a la fecha fin.");
        }

        return await _citaRepository.ObtenerPorPacienteAsync(idPaciente, fechaInicio, fechaFin);
    }

    public async Task<IEnumerable<CitaDetalleDto>> ObtenerAgendaMedicoAsync(int idMedico, DateOnly fechaInicio, DateOnly fechaFin)
    {
        if (fechaInicio > fechaFin)
        {
            throw new ArgumentException("La fecha de inicio no puede ser posterior a la fecha fin.");
        }

        return await _citaRepository.ObtenerPorMedicoAsync(idMedico, fechaInicio, fechaFin);
    }

    public async Task<bool> CancelarCitaAsync(int idCita, int idUsuarioToken, string rolUsuario)
    {
        var cita = await _citaRepository.ObtenerPorIdAsync(idCita);
        if (cita == null)
        {
            throw new KeyNotFoundException("La cita especificada no existe.");
        }

        if (cita.IdEstado != 1)
        {
            throw new InvalidOperationException("Solo se pueden cancelar citas en estado 'Programada'.");
        }

        // Validación de permisos: Solo el dueño de la cita, un Administrador o una Recepcionista pueden cancelar
        if (rolUsuario != "Administrador" && rolUsuario != "Recepcionista" && cita.IdPaciente != idUsuarioToken)
        {
            throw new UnauthorizedAccessException("No tienes autorización para cancelar esta cita.");
        }

        // Estado 4 = Cancelada
        return await _citaRepository.ActualizarEstadoAsync(idCita, 4);
    }

    public async Task<bool> ReprogramarCitaAsync(int idCita, ReprogramarCitaRequestDto req, int idUsuarioToken, string rolUsuario)
    {
        if (req.NuevaFecha < DateOnly.FromDateTime(DateTime.Now))
        {
            throw new ArgumentException("No se puede reprogramar una cita a una fecha pasada.");
        }

        var cita = await _citaRepository.ObtenerPorIdAsync(idCita);
        if (cita == null)
        {
            throw new KeyNotFoundException("La cita especificada no existe.");
        }

        if (cita.IdEstado != 1)
        {
            throw new InvalidOperationException("Solo se pueden reprogramar citas que se encuentren en estado 'Programada'.");
        }

        if (rolUsuario != "Administrador" && rolUsuario != "Recepcionista" && cita.IdPaciente != idUsuarioToken)
        {
            throw new UnauthorizedAccessException("No tienes autorización para reprogramar esta cita.");
        }

        try
        {
            return await _citaRepository.ReprogramarAsync(idCita, req.NuevaFecha, req.NuevaHoraInicio);
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            if (ex.ConstraintName != null && ex.ConstraintName.Contains("medico"))
            {
                throw new InvalidOperationException("La nueva franja horaria acaba de ser ocupada por otro paciente. Por favor, elige otra.");
            }

            if (ex.ConstraintName != null && ex.ConstraintName.Contains("paciente"))
            {
                throw new InvalidOperationException("Ya tienes otra cita programada exactamente a esa misma hora.");
            }

            throw new InvalidOperationException("Conflicto de horarios: la franja horaria seleccionada no está disponible.");
        }
    }

    public async Task<bool> CambiarEstadoCitaAsync(int idCita, int nuevoEstado)
    {
        var cita = await _citaRepository.ObtenerPorIdAsync(idCita);
        if (cita == null)
        {
            throw new KeyNotFoundException("La cita especificada no existe.");
        }

        if (nuevoEstado < 1 || nuevoEstado > 4)
        {
            throw new ArgumentException("El estado especificado no es válido.");
        }

        return await _citaRepository.ActualizarEstadoAsync(idCita, nuevoEstado);
    }
}