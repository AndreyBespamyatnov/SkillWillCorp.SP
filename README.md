# SkillWillCorp.SP

# TO ADD
add-spsolution -literalpath C:\Deployment\12092016\SkillWillCorp.SP.Offices.wsp
Install-SPSolution -Identity SkillWillCorp.SP.Offices.wsp -GacDeployment

# TO UPDATE
update-spsolution -literalpath C:\Deployment\12092016\SkillWillCorp.SP.Offices.wsp -Identity SkillWillCorp.SP.Offices.wsp -GacDeployment.

# TO ACTIVATE - On "Web" scoupe you can activate feature just with PS, because we have Jobs in Feature 
Enable-SPFeature -Identity f2fcbc29-9b41-4be2-971d-ef1d8e321ec5 -Url http://sw-sp13-2/Andrey_Bespamyatnov/
