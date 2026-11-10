using Domain.DTOs;
using Infrastructure.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Services;

public class EspecialidadService
{
    private readonly EspecialidadRepository _especialidadRepository;

    public EspecialidadService(EspecialidadRepository especialidadRepository)
    {
        _especialidadRepository = especialidadRepository;
    }

    public async Task<IEnumerable<EspecialidadDto>> ObtenerTodasAsync()
    {
        return await _especialidadRepository.ObtenerTodasAsync();
    }
}