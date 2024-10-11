using AutoMapper;
using ReviewVisualizer.Data.Models;
using ReviewVisualizer.Data.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReviewVisualizer.Data.Mapper
{
    public class MyMapper : Profile
    {
        public MyMapper()
        {
            CreateMap<Department, DepartmentCreateDTO>().ReverseMap();
            CreateMap<Teacher, TeacherCreateDTO>().ReverseMap();
            CreateMap<Reviewer, ReviewerCreateDTO>().ReverseMap();
        }
    }
}
