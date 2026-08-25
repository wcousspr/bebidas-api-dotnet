using AutoMapper;
using crudEbancoDist8.DTOs;
using crudEbancoDist8.Models;

namespace crudEbancoDist8.Mappings;

public class BebidasProfile : Profile
{
    public BebidasProfile()
    {
        // Entidade → DTO de leitura
        CreateMap<Bebidas, ReadBebidaDto>()
            .ForMember(
                destino => destino.Category,
                opcao => opcao.MapFrom(
                    origem => origem.Category.Name
                )
            );

        // DTO de criação → entidade
        CreateMap<CreateBebidaDto, Bebidas>()
            .ForMember(
                destino => destino.Id,
                opcao => opcao.Ignore()
            )
            .ForMember(
                destino => destino.Category,
                opcao => opcao.Ignore()
            );

        // DTO de atualização → entidade existente
        CreateMap<UpdateBebidaDto, Bebidas>()
            .ForMember(
                destino => destino.Id,
                opcao => opcao.Ignore()
            )
            .ForMember(
                destino => destino.Category,
                opcao => opcao.Ignore()
            );
    }
}