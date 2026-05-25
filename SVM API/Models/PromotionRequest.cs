using System.Collections.Generic;

namespace SVM_API.Models
{
    public class PromotionRequest
    {
        public List<int> StudentIds { get; set; }

        public int NewClassId { get; set; }

        public int NewSectionId { get; set; }

        public int NewSessionId { get; set; }
    }
}